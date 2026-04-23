using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;
using AquaGas.Api.Shared.Results;

namespace AquaGas.Api.Modules.Employee.Application.UseCases;

public interface IRegisterEmployee
{
    Task<Result<EmployeeWithUserResponse>> Execute(RegisterEmployeeInput data);
}