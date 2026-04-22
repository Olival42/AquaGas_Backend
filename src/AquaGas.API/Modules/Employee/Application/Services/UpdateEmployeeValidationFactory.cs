using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using AquaGas.Api.Modules.Auth.Domain.Enums;
using AquaGas.Api.Modules.Auth.Domain.Factories;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;
using AquaGas.Api.Modules.Employee.Domain.ValueObjects;
using AquaGas.Api.Shared.Domain.ValueObjects;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;

public static class UpdateEmployeeValidationFactory
{
    public static Result<UpdateEmployeeValidated> Combine(UpdateEmployeeInput data)
    {
        var errors = new List<Error>();

        EmployeeName? name = null;
        Email? email = null;
        Phone? phone = null;
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

        if(data.Role is not null)
        {
            var result = RoleFactory.Create(data.Role);
            if (result.IsFailure) errors.AddRange(result.Errors);
            else role = result.Value;
        }

        if (errors.Any())
            return Result<UpdateEmployeeValidated>.Fail(errors.ToArray());

        return Result<UpdateEmployeeValidated>.Success(
            new UpdateEmployeeValidated(name, email, phone, role)
        );
    }
}