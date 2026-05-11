using AquaGas.Auth.Application.Dtos.Responses;
using AquaGas.Auth.Domain.Repositories;
using AquaGas.Employee.Application.Dtos.Responses;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using Mapster;

namespace AquaGas.Employee.Application.UseCases;

public class GetByIdEmployee : IGetByIdEmployee
{

    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;

    public GetByIdEmployee(
        IEmployeeRepository employeeRepository,
        IUserRepository userRepositor)
    {
        _employeeRepository = employeeRepository;
        _userRepository = userRepositor;
    }

    public async Task<Result<EmployeeWithUserResponse>> Execute(Guid id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);

        if (employee is null)
            return Result<EmployeeWithUserResponse>.Fail(
                Error.NotFound("Employee not found")
            );

        var user = await _userRepository.GetByEmployeeIdAsync(id);

        if (user is null)
            return Result<EmployeeWithUserResponse>.Fail(
                Error.NotFound("User not found")
            );

        var employeeDto = employee.Adapt<EmployeeResponse>();

        var userDto = user is null
            ? null
            :user.Adapt<UserResponse>();

        return Result<EmployeeWithUserResponse>.Success(
            new EmployeeWithUserResponse(userDto!, employeeDto)
        );
    }
}