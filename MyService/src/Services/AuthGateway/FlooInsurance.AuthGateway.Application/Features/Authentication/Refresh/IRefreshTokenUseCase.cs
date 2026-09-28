using FlooInsurance.AuthGateway.Application.DTOs.Authentication;

namespace FlooInsurance.AuthGateway.Application.Features.Authentication.Refresh;

public interface IRefreshTokenUseCase
{
    Task<RefreshTokenResponseDto> ExecuteAsync(RefreshTokenRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default);
}
