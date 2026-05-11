using AquaGas.Shared.Results;
using AquaGas.Employee.Application.Dtos.Requests;
using AquaGas.Employee.Application.Dtos.Responses;

namespace AquaGas.Employee.Application.UseCases;

public interface IUpdateEmployee
{
    Task<Result<EmployeeWithUserResponse>> Execute(UpdateEmployeeInput data, Guid id);
}