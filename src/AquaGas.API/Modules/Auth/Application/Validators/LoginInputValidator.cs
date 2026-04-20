namespace AquaGas.Api.Modules.Auth.Application.Validators;

using AquaGas.Api.Modules.Auth.Application.Dtos.Requests;
using FluentValidation;

public class LoginInputValidator : AbstractValidator<LoginInput>
{
    public LoginInputValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .WithMessage("UserName is required");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required");
    }
}