using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using AquaGas.Api.Shared.Results;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;

namespace AquaGas.Api.Modules.Employee.Application.UseCases;

public interface IUpdateEmployee
{
    Task<Result<EmployeeWithUserResponse>> Execute(UpdateEmployeeInput data, Guid id);
}