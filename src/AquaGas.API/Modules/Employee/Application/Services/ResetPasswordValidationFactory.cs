using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using AquaGas.Api.Modules.Auth.Domain.ValueObjects;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;
using AquaGas.Api.Shared.Results;

public static class ResetPasswordValidation
{
    public static Result<ResetPasswordValidated> Combine(ResetPasswordInput data)
    {
        var password = Password.Create(data.NewPassword);

        var result = Result.Combine(password);

        if (result.IsFailure)
            return Result<ResetPasswordValidated>.Fail(result.Errors.ToArray());

        return Result<ResetPasswordValidated>.Success(
            new ResetPasswordValidated(password.Value!)
        );
    }
}