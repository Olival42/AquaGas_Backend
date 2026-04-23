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

        var response = employees.Select(e =>
            new EmployeeWithUserResponse(
                new UserResponse(
                    e.User!.Id,
                    e.User.UserName.Value,
                    e.User.Role
                ),
                new EmployeeResponse(
                    e.Id,
                    e.Name.Value,
                    e.CPF.Value,
                    e.Email.Value,
                    e.Phone.Value
                )
            )
        ).ToList();

        return Result<List<EmployeeWithUserResponse>>.Success(response);
    }
}