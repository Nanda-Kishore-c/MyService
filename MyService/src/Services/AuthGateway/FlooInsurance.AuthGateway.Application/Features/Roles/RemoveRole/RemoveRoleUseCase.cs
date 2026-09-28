using FlooInsurance.AuthGateway.Application.Common.Exceptions;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;
using FlooInsurance.AuthGateway.Domain.Constants;

namespace FlooInsurance.AuthGateway.Application.Features.Roles.RemoveRole;

public class RemoveRoleUseCase : IRemoveRoleUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveRoleUseCase(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(
        Guid targetUserId, 
        string roleName, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ValidationException("Role", "Role name cannot be empty.");
        }

        if (!RoleConstants.IsValidRole(roleName))
        {
            throw new ValidationException("Role", $"'{roleName}' is not a valid system role.");
        }

        // Prevent admin from removing their own Admin role to avoid accidental lockout
        if (_currentUserService.UserId.HasValue && 
            _currentUserService.UserId.Value == targetUserId && 
            string.Equals(roleName, RoleConstants.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Role", "Administrators cannot remove their own Admin role.");
        }

        var targetUser = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
        if (targetUser == null)
        {
            throw new NotFoundException("User", targetUserId);
        }

        var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
        if (role == null)
        {
            throw new NotFoundException("Role", roleName);
        }

        var userRole = await _roleRepository.GetUserRoleAsync(targetUserId, role.Id, cancellationToken);
        if (userRole == null)
        {
            throw new NotFoundException("UserRole", $"User {targetUserId} does not have role '{roleName}'.");
        }

        await _roleRepository.RemoveUserRoleAsync(userRole, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
