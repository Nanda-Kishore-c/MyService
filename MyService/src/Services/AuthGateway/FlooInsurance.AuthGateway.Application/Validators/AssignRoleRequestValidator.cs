using FluentValidation;
using FlooInsurance.AuthGateway.Application.DTOs.Roles;
using FlooInsurance.AuthGateway.Domain.Constants;

namespace FlooInsurance.AuthGateway.Application.Validators;

public class AssignRoleRequestValidator : AbstractValidator<AssignRoleRequestDto>
{
    public AssignRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role name is required.")
            .Must(role => RoleConstants.IsValidRole(role))
            .WithMessage($"Role must be one of: {string.Join(", ", RoleConstants.AllRoles)}.");
    }
}
