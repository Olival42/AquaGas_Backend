using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;
using FluentValidation;

namespace AquaGas.Api.Modules.Employee.Application.Validators;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordInput>
{
    public ResetPasswordValidator()
    {

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required")
                .OverridePropertyName("New Password");
    }
}