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
using AquaGas.API.Shared.Application.Services;
using AquaGas.API.Shared.Domain.Enums;

namespace AquaGas.Api.Modules.Employee.Application.UseCases;

public class RegisterEmployee : IRegisterEmployee
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContextService _userContextService;

    public RegisterEmployee(
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAuditLogService auditLogService,
        IUserContextService userContextService)
    {
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _auditLogService = auditLogService;
        _userContextService = userContextService;
    }

    public async Task<Result<EmployeeWithUserResponse>> Execute(RegisterEmployeeInput data)
    {
        var validation = RegisterEmployeeValidationFactory.Combine(data);
        if (validation.IsFailure)
            return Result<EmployeeWithUserResponse>.Fail(validation.Errors.ToArray());

        var userIdResult = _userContextService.GetUserId();
        if (userIdResult.IsFailure)
            return Result<EmployeeWithUserResponse>.Fail(userIdResult.Errors.ToArray());

        var userNameCtx = _userContextService.GetUserName();
        if (userNameCtx.IsFailure)
            return Result<EmployeeWithUserResponse>.Fail(userNameCtx.Errors.ToArray());

        var v = validation.Value!;
        var currentUserId = userIdResult.Value;

        var employee = await _employeeRepository.GetByCPFAsync(v.Cpf.Value);

        if (employee is not null)
            return await HandleExistingEmployee(employee, v, currentUserId);

        return await HandleNewEmployee(v, currentUserId);
    }

    private async Task<Result<EmployeeWithUserResponse>> HandleNewEmployee(
        RegisterEmployeeValidated v,
        Guid currentUserId)
    {
        var uniqueResult = await EnsureUniqueConstraints(
            v,
            ignoreEmployeeId: null,
            ignoreUserId: null
        );

        if (uniqueResult.IsFailure)
            return Result<EmployeeWithUserResponse>.Fail(uniqueResult.Errors.ToArray());

        var employee = new EmployeeEntity(v.Name, v.Cpf, v.Email, v.Phone);

        var user = new User(
            v.UserName,
            _passwordHasher.Hash(v.Password.Value),
            v.Role,
            employee.Id
        );

        employee.AssignUser(user);
        await _employeeRepository.AddAsync(employee);
        await _employeeRepository.SaveChangesAsync();

        await LogAudit(currentUserId, v, AuditAction.CREATE, employee, null);

        return Success(employee, user);
    }

    private async Task<Result<EmployeeWithUserResponse>> HandleExistingEmployee(
        EmployeeEntity employee,
        RegisterEmployeeValidated v,
        Guid currentUserId)
    {
        if (employee.IsActive)
            return Result<EmployeeWithUserResponse>.Fail(
                Error.Conflict("CPF already registered")
            );

        var uniqueResult = await EnsureUniqueConstraints(
            v,
            ignoreEmployeeId: employee.Id,
            ignoreUserId: employee.User?.Id
        );

        if (uniqueResult.IsFailure)
            return Result<EmployeeWithUserResponse>.Fail(uniqueResult.Errors.ToArray());

        var oldSnapshot = BuildSnapshot(employee);

        employee.Reactivate();
        employee.Update(v.Name, v.Email, v.Phone);

        var passwordHash = _passwordHasher.Hash(v.Password.Value);

        if (employee.User is null)
        {
            var user = new User(
                v.UserName,
                passwordHash,
                v.Role,
                employee.Id
            );

            employee.AssignUser(user);
        }
        else
        {
            employee.User.Reactive();
            employee.User.ChangeUserName(v.UserName);
            employee.User.ChangeRole(v.Role);
            employee.User.ChangePassword(v.Password, _passwordHasher);
        }

        _employeeRepository.Update(employee);
        await _employeeRepository.SaveChangesAsync();

        await LogAudit(currentUserId, v, AuditAction.UPDATE, employee, oldSnapshot);

        return Success(employee, employee.User!);
    }

    private async Task<Result> EnsureUniqueConstraints(
     RegisterEmployeeValidated v,
     Guid? ignoreEmployeeId,
     Guid? ignoreUserId)
    {
        var emailExists = await _employeeRepository.AnyByEmailAsync(
            v.Email.Value,
            ignoreEmployeeId ?? Guid.Empty
        );

        if (emailExists)
            return Result.Fail(
                Error.Conflict("Email already registered")
            );

        var usernameExists = await _userRepository.AnyByUserNameAsync(
            v.UserName.Value,
            ignoreUserId ?? Guid.Empty
        );

        if (usernameExists)
            return Result.Fail(
                Error.Conflict("Username already registered")
            );

        return Result.Success();
    }

    private async Task LogAudit(
        Guid userId,
        RegisterEmployeeValidated v,
        AuditAction action,
        EmployeeEntity employee,
        object? oldValues)
    {
        await _auditLogService.LogAsync(
            userId,
            v.UserName.Value,
            action,
            "Employee",
            employee.Id,
            oldValues,
            new
            {
                Employee = new { v.Name, v.Cpf, v.Email, v.Phone },
                User = new { v.UserName, v.Role }
            }
        );
    }

    private static Result<EmployeeWithUserResponse> Success(EmployeeEntity employee, User user)
    {
        return Result<EmployeeWithUserResponse>.Success(
            new EmployeeWithUserResponse(
                user.Adapt<UserResponse>(),
                employee.Adapt<EmployeeResponse>()
            )
        );
    }

    private static object BuildSnapshot(EmployeeEntity e)
    {
        return new
        {
            Employee = new
            {
                e.Name,
                CPF = e.CPF.Value,
                Email = e.Email.Value,
                e.Phone.Value,
                e.IsActive
            },
            User = e.User is null
                ? null
                : new
                {
                    UserName = e.User.UserName.Value,
                    e.User.Role,
                    e.User.IsActive
                }
        };
    }
}