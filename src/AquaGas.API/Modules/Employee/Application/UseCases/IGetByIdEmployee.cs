using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using AquaGas.Api.Shared.Results;

namespace AquaGas.Api.Modules.Employee.Application.UseCases;

public interface IGetByIdEmployee
{
    Task<Result<EmployeeWithUserResponse>> Execute(Guid id);
}