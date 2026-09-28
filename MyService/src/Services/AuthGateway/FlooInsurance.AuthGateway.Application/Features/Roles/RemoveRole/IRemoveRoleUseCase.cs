namespace FlooInsurance.AuthGateway.Application.Features.Roles.RemoveRole;

public interface IRemoveRoleUseCase
{
    Task ExecuteAsync(Guid targetUserId, string roleName, CancellationToken cancellationToken = default);
}
