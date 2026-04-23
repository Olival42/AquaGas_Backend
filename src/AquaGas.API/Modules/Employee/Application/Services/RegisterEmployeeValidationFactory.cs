using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using AquaGas.Api.Modules.Auth.Domain.Factories;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Shared.Domain.ValueObjects;
using AquaGas.Api.Shared.Results;

public static class RegisterEmployeeValidationFactory
{
    public static Result<RegisterEmployeeValidated> Combine(RegisterEmployeeInput data)
    {
        var name = EmployeeName.Create(data.Employee.Name);
        var cpf = Cpf.Create(data.Employee.Cpf);
        var email = Email.Create(data.Employee.Email);
        var phone = Phone.Create(data.Employee.Phone);

        var username = UserName.Create(data.User.UserName);
        var password = Password.Create(data.User.Password);
        var role = RoleFactory.Create(data.User.Role?.ToString() ?? "");

        var result = Result.Combine(
            name, cpf, email, phone, username, password, role
        );

        if (result.IsFailure)
            return Result<RegisterEmployeeValidated>.Fail(result.Errors.ToArray());

        return Result<RegisterEmployeeValidated>.Success(
            new RegisterEmployeeValidated(
                name.Value!,
                cpf.Value!,
                email.Value!,
                phone.Value!,
                username.Value!,
                password.Value!,
                role.Value
            )
        );
    }
}