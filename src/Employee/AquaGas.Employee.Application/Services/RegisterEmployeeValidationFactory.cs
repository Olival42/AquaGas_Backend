using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Auth.Domain.Factories;
using AquaGas.Auth.Domain.ValueObjects;
using AquaGas.Employee.Application.Dtos.Requests;
using AquaGas.Employee.Application.Dtos.Responses;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Results;

namespace AquaGas.Employee.Application.Services;

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