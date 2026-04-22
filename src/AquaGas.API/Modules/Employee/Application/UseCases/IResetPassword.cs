using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;
using AquaGas.Api.Shared.Results;

namespace AquaGas.Api.Modules.Employee.Application.UseCases;

public interface IResetPassword
{
    Task<Result<object>> Execute(Guid employeeId, ResetPasswordInput input);
}