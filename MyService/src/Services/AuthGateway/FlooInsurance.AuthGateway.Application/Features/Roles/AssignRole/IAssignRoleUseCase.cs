using FlooInsurance.AuthGateway.Application.DTOs.Roles;

namespace FlooInsurance.AuthGateway.Application.Features.Roles.AssignRole;

public interface IAssignRoleUseCase
{
    Task ExecuteAsync(Guid targetUserId, AssignRoleRequestDto request, CancellationToken cancellationToken = default);
}
