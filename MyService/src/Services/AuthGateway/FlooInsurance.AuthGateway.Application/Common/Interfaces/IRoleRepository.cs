using FlooInsurance.AuthGateway.Domain.Entities;

namespace FlooInsurance.AuthGateway.Application.Common.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRolesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserRole?> GetUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default);
    Task RemoveUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default);
}
