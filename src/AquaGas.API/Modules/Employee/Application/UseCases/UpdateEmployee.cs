using AquaGas.Api.Modules.Employee.Domain.Repositories;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;
using Mapster;
using AquaGas.Api.Modules.Auth.Application.Dtos.Responses;
using AquaGas.API.Shared.Application.Services;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.API.Shared.Domain.Enums;
using AquaGas.Api.Modules.Auth.Domain.Repositories;

namespace AquaGas.Api.Modules.Employee.Application.UseCases;

public class UpdateEmployee : IUpdateEmployee
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContextService _userContextService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public UpdateEmployee(
        IEmployeeRepository employeeRepository,
        IAuditLogService auditLogService,
        IUserContextService userContextService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _employeeRepository = employeeRepository;
        _auditLogService = auditLogService;
        _userContextService = userContextService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result<EmployeeWithUserResponse>> Execute(UpdateEmployeeInput data, Guid id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);

        if (employee is null)
            return Result<EmployeeWithUserResponse>.Fail(
                Error.NotFound("Employee not found"));

        var currentUser = _userContextService.GetUserId();

        if (currentUser.IsFailure)
            return Result<EmployeeWithUserResponse>.Fail(currentUser.Errors.ToArray());

        var validation = UpdateEmployeeValidationFactory.Combine(data);

        if (validation.IsFailure)
            return Result<EmployeeWithUserResponse>.Fail(validation.Errors.ToArray());

        var v = validation.Value!;

        var oldValues = new
        {
            employee.Name,
            employee.Email,
            employee.Phone,
            employee.User!.Role
        };

        employee.Update(v.Name, v.Email, v.Phone);

        if (v.Role is not null && employee.User.Role != v.Role.Value)
        {
            employee.User.UpdateRole(v.Role!.Value);
            await _refreshTokenRepository.RevokeAllByUserId(employee.User.Id);
        }

        _employeeRepository.Update(employee);
        await _employeeRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            userId: currentUser.Value!,
            userName: null,
            action: AuditAction.UPDATE,
            entityType: "Employee",
            entityId: employee.Id,
            oldValues: oldValues,
            newValues: new
            {
                employee.Name,
                employee.Email,
                employee.Phone,
                employee.User.Role
                
            }
        );

        return Result<EmployeeWithUserResponse>.Success(new EmployeeWithUserResponse(
            employee.User.Adapt<UserResponse>(),
            employee.Adapt<EmployeeResponse>()
        ));
    }
}