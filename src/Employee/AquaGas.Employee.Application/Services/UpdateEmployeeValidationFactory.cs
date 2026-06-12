using AquaGas.Auth.Application.Dtos.Responses;
using AquaGas.Auth.Domain.Enums;
using AquaGas.Auth.Domain.Factories;
using AquaGas.Auth.Domain.ValueObjects;
using AquaGas.Employee.Application.Dtos.Requests;
using AquaGas.Employee.Domain.ValueObjects;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
namespace AquaGas.Employee.Application.Services;

public static class UpdateEmployeeValidationFactory
{
    public static Result<UpdateEmployeeValidated> Combine(UpdateEmployeeInput data)
    {
        var errors = new List<Error>();

        EmployeeName? name = null;
        Email? email = null;
        Phone? phone = null;
        UserName? userName = null;
        Password? password = null;
        Role? role = null;

        if (data.Name is not null)
        {
            var result = EmployeeName.Create(data.Name);
            if (result.IsFailure) errors.AddRange(result.Errors);
            else name = result.Value;
        }

        if (data.Email is not null)
        {
            var result = Email.Create(data.Email);
            if (result.IsFailure) errors.AddRange(result.Errors);
            else email = result.Value;
        }

        if (data.Phone is not null)
        {
            var result = Phone.Create(data.Phone);
            if (result.IsFailure) errors.AddRange(result.Errors);
            else phone = result.Value;
        }

        if (data.UserName is not null)
        {
            var result = UserName.Create(data.UserName);
            if (result.IsFailure) errors.AddRange(result.Errors);
            else userName = result.Value;
        }

        if (data.NewPassword is not null)
        {
            var result = Password.Create(data.NewPassword);
            if (result.IsFailure) errors.AddRange(result.Errors);
            else password = result.Value;
        }

        if (data.Role is not null)
        {
            var result = RoleFactory.Create(data.Role);
            if (result.IsFailure) errors.AddRange(result.Errors);
            else role = result.Value;
        }

        if (errors.Any())
            return Result<UpdateEmployeeValidated>.Fail(errors.ToArray());

        return Result<UpdateEmployeeValidated>.Success(
            new UpdateEmployeeValidated(
                name,
                email,
                phone,
                userName,
                password,
                role
            )
        );
    }
}