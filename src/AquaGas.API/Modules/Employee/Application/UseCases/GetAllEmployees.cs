using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using AquaGas.Api.Modules.Employee.Domain.Repositories;
using AquaGas.Api.Shared.Results;
using Mapster;

namespace AquaGas.Api.Modules.Employee.Application.UseCases;

public class GetAllEmployees : IGetAllEmployees
{
    private readonly IEmployeeRepository _employeeRepository;

    public GetAllEmployees(
        IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<List<EmployeeWithUserResponse>>> Execute()
    {
        var employees = await _employeeRepository.GetAllAsync();

        var response = (employees ?? []).Select(e =>
            new EmployeeWithUserResponse(
                e.User.Adapt<UserResponse>()!,
                e.Adapt<EmployeeResponse>()
            )
        ).ToList();

        return Result<List<EmployeeWithUserResponse>>.Success(response);
    }
}