using AquaGas.Employee.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Employee.Application.UseCases;

public interface IGetByIdEmployee
{
    Task<Result<EmployeeWithUserResponse>> Execute(Guid id);
}