using FluentValidation;
using FlooInsurance.AuthGateway.Application.DTOs.Authentication;

namespace FlooInsurance.AuthGateway.Application.Validators;

public class LogoutRequestValidator : AbstractValidator<LogoutRequestDto>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
