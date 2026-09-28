using FluentValidation;
using FlooInsurance.AuthGateway.Application.Common.Exceptions;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;
using FlooInsurance.AuthGateway.Application.DTOs.Authentication;
using FlooInsurance.AuthGateway.Domain.Entities;
using ValidationException = FlooInsurance.AuthGateway.Application.Common.Exceptions.ValidationException;

namespace FlooInsurance.AuthGateway.Application.Features.Authentication.Login;

public class LoginUserUseCase : ILoginUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<LoginRequestDto> _validator;

    public LoginUserUseCase(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        IValidator<LoginRequestDto> validator)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<LoginResponseDto> ExecuteAsync(
        LoginRequestDto request, 
        string? ipAddress = null, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user == null)
        {
            throw new AuthenticationException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new AuthenticationException("Account is inactive. Please contact support.");
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new AuthenticationException("Invalid email or password.");
        }

        var roles = await _roleRepository.GetRolesByUserIdAsync(user.Id, cancellationToken);

        // Generate Access Token (JWT)
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var accessTokenExpiresAt = _tokenService.GetAccessTokenExpiration();

        // Generate Refresh Token
        var rawRefreshToken = _refreshTokenService.GenerateRefreshToken();
        var refreshTokenHash = _refreshTokenService.HashToken(rawRefreshToken);
        var refreshExpiresAt = _refreshTokenService.GetRefreshTokenExpiration();

        var refreshToken = new RefreshToken(
            user.Id,
            refreshTokenHash,
            _dateTimeProvider.UtcNow,
            refreshExpiresAt,
            ipAddress);

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = accessTokenExpiresAt,
            TokenType = "Bearer",
            User = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Roles = roles,
                CreatedAt = user.CreatedAt
            }
        };
    }
}
