using FlooInsurance.AuthGateway.Application.DTOs.Authentication;

namespace FlooInsurance.AuthGateway.Application.Features.Authentication.CurrentUser;

public interface IGetCurrentUserUseCase
{
    Task<CurrentUserResponseDto> ExecuteAsync(CancellationToken cancellationToken = default);
}
