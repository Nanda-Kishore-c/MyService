using FlooInsurance.AuthGateway.Application.DTOs.Authentication;

namespace FlooInsurance.AuthGateway.Application.Features.Authentication.Login;

public interface ILoginUserUseCase
{
    Task<LoginResponseDto> ExecuteAsync(LoginRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default);
}
