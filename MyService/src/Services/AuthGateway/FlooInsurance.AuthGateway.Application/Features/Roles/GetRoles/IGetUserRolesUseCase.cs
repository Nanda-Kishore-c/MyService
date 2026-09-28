namespace FlooInsurance.AuthGateway.Application.Features.Roles.GetRoles;

public interface IGetUserRolesUseCase
{
    Task<IReadOnlyList<string>> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default);
}
