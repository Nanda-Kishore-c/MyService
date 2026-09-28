using FluentValidation;
using FlooInsurance.AuthGateway.Application.Common.Exceptions;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;
using FlooInsurance.AuthGateway.Application.DTOs.Authentication;
using FlooInsurance.AuthGateway.Domain.Entities;
using ValidationException = FlooInsurance.AuthGateway.Application.Common.Exceptions.ValidationException;

namespace FlooInsurance.AuthGateway.Application.Features.Authentication.Refresh;

public class RefreshTokenUseCase : IRefreshTokenUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RefreshTokenRequestDto> _validator;

    public RefreshTokenUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IRefreshTokenService refreshTokenService,
        ITokenService tokenService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        IValidator<RefreshTokenRequestDto> validator)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _refreshTokenService = refreshTokenService;
        _tokenService = tokenService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<RefreshTokenResponseDto> ExecuteAsync(
        RefreshTokenRequestDto request, 
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

        var tokenHash = _refreshTokenService.HashToken(request.RefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (existingToken == null)
        {
            throw new AuthenticationException("Invalid refresh token.");
        }

        // Token reuse detection
        if (existingToken.IsRevoked)
        {
            // Revoke all remaining active tokens for the user to mitigate compromise
            await _refreshTokenRepository.RevokeAllForUserAsync(existingToken.UserId, ipAddress, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new AuthenticationException("Revoked refresh token reuse detected. All user sessions have been terminated.");
        }

        // Expiration check
        if (existingToken.IsExpiredAt(_dateTimeProvider.UtcNow))
        {
            throw new AuthenticationException("Refresh token has expired. Please log in again.");
        }

        var user = await _userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new AuthenticationException("User account is inactive or not found.");
        }

        // Generate new refresh token
        var newRawRefreshToken = _refreshTokenService.GenerateRefreshToken();
        var newTokenHash = _refreshTokenService.HashToken(newRawRefreshToken);
        var newRefreshExpiresAt = _refreshTokenService.GetRefreshTokenExpiration();

        // Rotate: revoke the old token with reference to the replacement token hash
        existingToken.Revoke(ipAddress, newTokenHash);
        await _refreshTokenRepository.UpdateAsync(existingToken, cancellationToken);

        // Store new active refresh token
        var newRefreshToken = new RefreshToken(
            user.Id,
            newTokenHash,
            _dateTimeProvider.UtcNow,
            newRefreshExpiresAt,
            ipAddress);

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        // Generate new JWT access token
        var roles = await _roleRepository.GetRolesByUserIdAsync(user.Id, cancellationToken);
        var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
        var newAccessTokenExpiresAt = _tokenService.GetAccessTokenExpiration();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRawRefreshToken,
            ExpiresAt = newAccessTokenExpiresAt,
            TokenType = "Bearer"
        };
    }
}
