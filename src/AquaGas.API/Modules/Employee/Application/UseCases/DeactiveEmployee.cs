using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Auth.Domain.Repositories;
using AquaGas.Api.Modules.Employee.Domain.Repositories;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Shared.Application.Services;
using AquaGas.API.Shared.Domain.Enums;

namespace AquaGas.Api.Modules.Employee.Application.UseCases;

public class DeactiveEmployee : IDeactiveEmployee
{

    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserContextService _userContextService;
    private readonly IAuditLogService _auditLogService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public DeactiveEmployee(
    IEmployeeRepository employeeRepository,
    IUserContextService userContextService,
    IAuditLogService auditLogService,
    IRefreshTokenRepository refreshTokenRepository)
    {
        _employeeRepository = employeeRepository;
        _userContextService = userContextService;
        _auditLogService = auditLogService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result<object>> Execute(Guid id)
    {
        var currentUser = _userContextService.GetUserId();
        if (currentUser.IsFailure)
            return Result<object>.Fail(currentUser.Errors.ToArray());

        var userNameCtx = _userContextService.GetUserName();
        if (userNameCtx.IsFailure)
            return Result<object>.Fail(userNameCtx.Errors.ToArray());

        var employee = await _employeeRepository.GetByIdAsync(id);

        if (employee is null)
            return Result<object>.Fail(Error.NotFound("Employee not found"));

        if (employee.User is not null && employee.User.Id == currentUser.Value)
            return Result<object>.Fail(
                Error.Conflict("You cannot deactivate your own account")
            );

        var oldValues = new
        {
            employee.Id,
            employee.Name,
            employee.Email,
            employee.IsActive,
            employee.User?.Role
        };

        employee.Deactive();

        _employeeRepository.Update(employee);

        if (employee.User is not null)
            await _refreshTokenRepository.RevokeAllByUserId(employee.User.Id);
            
        await _employeeRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            userId: currentUser.Value,
            userName: userNameCtx.Value,
            action: AuditAction.DEACTIVATE,
            entityType: "Employee",
            entityId: employee.Id,
            oldValues: oldValues,
            newValues: new { employee.Id, employee.IsActive }
        );

        return Result<object>.Success(new object());
    }
}