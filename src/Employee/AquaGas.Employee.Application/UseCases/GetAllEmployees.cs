using AquaGas.Auth.Application.Dtos.Responses;
using AquaGas.Auth.Domain.Repositories;
using AquaGas.Employee.Application.Dtos.Responses;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Shared.Results;
using Mapster;

namespace AquaGas.Employee.Application.UseCases;

public class GetAllEmployees : IGetAllEmployees
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;

    public GetAllEmployees(
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository)
    {
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<List<EmployeeWithUserResponse>>> Execute()
    {
        var employees = await _employeeRepository.GetAllAsync();

        var response = new List<EmployeeWithUserResponse>();

        foreach (var employee in employees ?? [])
        {
            var user = await _userRepository.GetByEmployeeIdAsync(employee.Id);

            if (user is null)
                continue;

            response.Add(
                new EmployeeWithUserResponse(
                    user.Adapt<UserResponse>(),
                    employee.Adapt<EmployeeResponse>()
                )
            );
        }

        return Result<List<EmployeeWithUserResponse>>.Success(response);
    }
}