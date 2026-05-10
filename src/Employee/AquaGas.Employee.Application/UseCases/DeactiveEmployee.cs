using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Repositories;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Application.Services;

namespace AquaGas.Employee.Application.UseCases;

public class DeactiveEmployee : IDeactiveEmployee
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserContextService _userContextService;
    private readonly IAuditLogService _auditLogService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public DeactiveEmployee(
    IEmployeeRepository employeeRepository,
    IUserContextService userContextService,
    IAuditLogService auditLogService,
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository)
    {
        _employeeRepository = employeeRepository;
        _userContextService = userContextService;
        _auditLogService = auditLogService;
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
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

        var user = await _userRepository.GetByEmployeeIdAsync(id);

        if (user is null)
            return Result<object>.Fail(Error.NotFound("User not found"));

        if (user is not null && user.Id == currentUser.Value)
            return Result<object>.Fail(
                Error.Conflict("You cannot deactivate your own account")
            );

        var oldValues = new
        {
            employee.Id,
            employee.Name,
            employee.Email,
            employee.IsActive,
            user?.Role
        };

        employee.Deactivate();
        user!.Deactive();

        _employeeRepository.Update(employee);
        _userRepository.Update(user);

        if (user is not null)
            await _refreshTokenRepository.RevokeAllByUserId(user.Id);

        await _employeeRepository.SaveChangesAsync();
        await _userRepository.SaveChangesAsync();

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