using AquaGas.Employee.Application.Dtos.Requests;
using FluentValidation;

namespace AquaGas.Employee.Application.Validators;

public class RegisterEmployeeValidator : AbstractValidator<RegisterEmployeeInput>
{
    public RegisterEmployeeValidator()
    {
        RuleFor(x => x.User)
            .NotNull()
            .WithMessage("User is required");

        RuleFor(x => x.Employee)
            .NotNull().WithMessage("Employee is required");

        When(x => x.User != null, () =>
        {
            RuleFor(x => x.User.UserName)
                .NotEmpty()
                .WithMessage("Username is required")
                .OverridePropertyName("Username");

            RuleFor(x => x.User.Password)
                .NotEmpty().WithMessage("Password is required")
                .OverridePropertyName("Password");

            RuleFor(x => x.User.Role)
                .NotEmpty()
                .WithMessage("Role is required")
                .OverridePropertyName("Role");
        });

        When(x => x.Employee != null, () =>
        {
            RuleFor(x => x.Employee.Name)
                .NotEmpty()
                .WithMessage("Name is required")
                .OverridePropertyName("Name");

            RuleFor(x => x.Employee.Cpf)
                .NotEmpty()
                .WithMessage("CPF is required")
                .OverridePropertyName("CPF");

            RuleFor(x => x.Employee.Email)
                .NotEmpty().WithMessage("Email is required")
                .OverridePropertyName("Email");

            RuleFor(x => x.Employee.Phone)
                .NotEmpty()
                .WithMessage("Phone is required")
                .OverridePropertyName("Phone");
        });
    }
}