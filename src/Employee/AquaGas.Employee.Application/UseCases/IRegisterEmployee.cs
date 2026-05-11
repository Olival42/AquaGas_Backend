using AquaGas.Employee.Application.Dtos.Requests;
using AquaGas.Employee.Application.Dtos.Responses;
using AquaGas.Shared.Results;

namespace AquaGas.Employee.Application.UseCases;

public interface IRegisterEmployee
{
    Task<Result<EmployeeWithUserResponse>> Execute(RegisterEmployeeInput data);
}