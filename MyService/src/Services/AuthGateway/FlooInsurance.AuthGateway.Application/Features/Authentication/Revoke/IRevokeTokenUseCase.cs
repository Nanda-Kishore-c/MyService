using FlooInsurance.AuthGateway.Application.DTOs.Authentication;

namespace FlooInsurance.AuthGateway.Application.Features.Authentication.Revoke;

public interface IRevokeTokenUseCase
{
    Task ExecuteAsync(RevokeTokenRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default);
}
