using FluentValidation;
using FlooInsurance.AuthGateway.Application.Common.Exceptions;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;
using FlooInsurance.AuthGateway.Application.DTOs.Authentication;
using ValidationException = FlooInsurance.AuthGateway.Application.Common.Exceptions.ValidationException;

namespace FlooInsurance.AuthGateway.Application.Features.Authentication.Logout;

public class LogoutUseCase : ILogoutUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<LogoutRequestDto> _validator;

    public LogoutUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenService refreshTokenService,
        IUnitOfWork unitOfWork,
        IValidator<LogoutRequestDto> validator)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenService = refreshTokenService;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task ExecuteAsync(
        LogoutRequestDto request, 
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
            throw new AuthenticationException("Invalid refresh token.");
        }

        if (!token.IsRevoked)
        {
            token.Revoke(ipAddress);
            await _refreshTokenRepository.UpdateAsync(token, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
