using FlooInsurance.AuthGateway.Application.DTOs.Authentication;

namespace FlooInsurance.AuthGateway.Application.Features.Authentication.Register;

public interface IRegisterUserUseCase
{
    Task<RegisterResponseDto> ExecuteAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
}
