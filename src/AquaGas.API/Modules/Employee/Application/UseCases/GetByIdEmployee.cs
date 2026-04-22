using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using AquaGas.Api.Modules.Auth.Domain.Models;
using AquaGas.Api.Modules.Auth.Domain.Repositories;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;
using AquaGas.Api.Modules.Employee.Domain.Repositories;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;
using Mapster;
using EmployeeEntity = AquaGas.Api.Modules.Employee.Domain.Models.Employee;
using AquaGas.Api.Shared.Responses;

namespace AquaGas.Api.Modules.Employee.Application.UseCases;

public class GetByIdEmployee : IGetByIdEmployee
{

    private readonly IEmployeeRepository _employeeRepository;

    public GetByIdEmployee(
        IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<EmployeeWithUserResponse>> Execute(Guid id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);

        if(employee is null)
            return Result<EmployeeWithUserResponse>.Fail(
                    Error.NotFound("Employee not found"));

        var employeeDto = employee.Adapt<EmployeeResponse>();
        var userDto = employee.User.Adapt<UserResponse>();

        return Result<EmployeeWithUserResponse>.Success(
            new EmployeeWithUserResponse(userDto!, employeeDto));
    }
}