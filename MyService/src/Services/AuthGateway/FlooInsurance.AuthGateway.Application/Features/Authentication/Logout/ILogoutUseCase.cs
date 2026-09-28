using FlooInsurance.AuthGateway.Application.DTOs.Authentication;

namespace FlooInsurance.AuthGateway.Application.Features.Authentication.Logout;

public interface ILogoutUseCase
{
    Task ExecuteAsync(LogoutRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default);
}
