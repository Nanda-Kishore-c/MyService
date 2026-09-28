using FlooInsurance.AuthGateway.Application.Common.Exceptions;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;

namespace FlooInsurance.AuthGateway.Application.Features.Roles.GetRoles;

public class GetUserRolesUseCase : IGetUserRolesUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public GetUserRolesUseCase(
        IUserRepository userRepository,
        IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task<IReadOnlyList<string>> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        return await _roleRepository.GetRolesByUserIdAsync(userId, cancellationToken);
    }
}
