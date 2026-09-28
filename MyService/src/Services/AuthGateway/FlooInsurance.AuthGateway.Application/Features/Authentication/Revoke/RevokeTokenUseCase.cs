using FluentValidation;
using FlooInsurance.AuthGateway.Application.Common.Exceptions;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;
using FlooInsurance.AuthGateway.Application.DTOs.Authentication;
using FlooInsurance.AuthGateway.Domain.Constants;
using ValidationException = FlooInsurance.AuthGateway.Application.Common.Exceptions.ValidationException;

namespace FlooInsurance.AuthGateway.Application.Features.Authentication.Revoke;

public class RevokeTokenUseCase : IRevokeTokenUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RevokeTokenRequestDto> _validator;

    public RevokeTokenUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenService refreshTokenService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IValidator<RevokeTokenRequestDto> validator)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenService = refreshTokenService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task ExecuteAsync(
        RevokeTokenRequestDto request, 
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
        var token = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token == null)
        {
            throw new NotFoundException("RefreshToken", "Token not found.");
        }

        // Ensure user can only revoke their own tokens unless Admin
        if (_currentUserService.IsAuthenticated)
        {
            var isUserAdmin = _currentUserService.Roles.Contains(RoleConstants.Admin, StringComparer.OrdinalIgnoreCase);
            if (!isUserAdmin && token.UserId != _currentUserService.UserId)
            {
                throw new AuthorizationException("You are not authorized to revoke tokens belonging to another user.");
            }
        }

        if (!token.IsRevoked)
        {
            token.Revoke(ipAddress);
            await _refreshTokenRepository.UpdateAsync(token, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
