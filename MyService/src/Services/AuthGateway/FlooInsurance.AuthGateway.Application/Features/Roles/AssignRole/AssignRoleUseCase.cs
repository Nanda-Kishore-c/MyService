using FluentValidation;
using FlooInsurance.AuthGateway.Application.Common.Exceptions;
using FlooInsurance.AuthGateway.Application.Common.Interfaces;
using FlooInsurance.AuthGateway.Application.DTOs.Roles;
using FlooInsurance.AuthGateway.Domain.Constants;
using FlooInsurance.AuthGateway.Domain.Entities;
using ValidationException = FlooInsurance.AuthGateway.Application.Common.Exceptions.ValidationException;

namespace FlooInsurance.AuthGateway.Application.Features.Roles.AssignRole;

public class AssignRoleUseCase : IAssignRoleUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<AssignRoleRequestDto> _validator;

    public AssignRoleUseCase(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        IValidator<AssignRoleRequestDto> validator)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task ExecuteAsync(
        Guid targetUserId, 
        AssignRoleRequestDto request, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        if (!RoleConstants.IsValidRole(request.Role))
        {
            throw new ValidationException("Role", $"'{request.Role}' is not a valid system role.");
        }

        var targetUser = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
        if (targetUser == null)
        {
            throw new NotFoundException("User", targetUserId);
        }

        var role = await _roleRepository.GetByNameAsync(request.Role, cancellationToken);
        if (role == null)
        {
            throw new NotFoundException("Role", request.Role);
        }

        var existingUserRole = await _roleRepository.GetUserRoleAsync(targetUserId, role.Id, cancellationToken);
        if (existingUserRole != null)
        {
            return; // Role already assigned, idempotent operation
        }

        var userRole = new UserRole(targetUserId, role.Id);
        await _roleRepository.AddUserRoleAsync(userRole, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
