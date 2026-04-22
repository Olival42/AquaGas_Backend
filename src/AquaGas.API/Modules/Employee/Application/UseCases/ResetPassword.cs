using AquaGas.Api.Modules.Auth.Domain.Repositories;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Employee.Application.Dtos.Requests;
using AquaGas.Api.Modules.Employee.Domain.Repositories;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Shared.Application.Services;
using AquaGas.API.Shared.Domain.Enums;

namespace AquaGas.Api.Modules.Employee.Application.UseCases;

public class ResetPassword : IResetPassword
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _auditLogService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserContextService _userContextService;

    public ResetPassword(
    IEmployeeRepository employeeRepository,
    IPasswordHasher passwordHasher,
    IAuditLogService auditLogService,
     IRefreshTokenRepository refreshTokenRepository,
      IUserContextService userContextService)
    {
        _employeeRepository = employeeRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _auditLogService = auditLogService;
        _userContextService = userContextService;
    }

    public async Task<Result<object>> Execute(Guid employeeId, ResetPasswordInput input)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);

        if (employee is null)
            return Result<object>.Fail(Error.NotFound("Employee not found"));

        var validation = ResetPasswordValidation.Combine(input);

        if (validation.IsFailure)
            return Result<object>.Fail(validation.Errors.ToArray());

        var currentUser = _userContextService.GetUserId();

        if (currentUser.IsFailure)
            return Result<object>.Fail(currentUser.Errors.ToArray());

        var v = validation.Value!;
        var hash = _passwordHasher.Hash(v.Password.Value);

        employee.User!.UpdatePassword(hash);

        await _refreshTokenRepository.RevokeAllByUserId(employee.User.Id);

        await _employeeRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            userId: currentUser.Value,
            userName: null,
            action: AuditAction.UPDATE,
            entityType: "Employee",
            entityId: employee.Id,
            oldValues: new { PasswordChanged = false },
            newValues: new { PasswordChanged = true }
        );

        return Result<object>.Success(new object());
    }
}