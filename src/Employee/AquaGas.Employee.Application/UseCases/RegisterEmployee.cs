using AquaGas.Application.Services;
using AquaGas.Auth.Application.Dtos.Responses;
using AquaGas.Auth.Application.Services;
using AquaGas.Auth.Domain.Models;
using AquaGas.Auth.Domain.Repositories;
using AquaGas.Auth.Domain.Services;
using AquaGas.Employee.Application.Dtos.Requests;
using AquaGas.Employee.Application.Dtos.Responses;
using AquaGas.Employee.Application.Services;
using AquaGas.Employee.Domain.Repositories;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using Mapster;
using EmployeeEntity = AquaGas.Employee.Domain.Models.Employee;

namespace AquaGas.Employee.Application.UseCases;

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

        var employee = new EmployeeEntity(
            v.Name,
            v.Cpf,
            v.Email,
            v.Phone
        );

        var user = new User(
            v.UserName,
            _passwordHasher.Hash(v.Password.Value),
            v.Role,
            employee.Id
        );

        employee.AssignUserId(user.Id);

        await _employeeRepository.AddAsync(employee);
        await _userRepository.AddAsync(user);

        await _userRepository.SaveChangesAsync();
        await _employeeRepository.SaveChangesAsync();

        await LogAudit(
            currentUserId,
            v,
            AuditAction.CREATE,
            employee,
            null
        );

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

        var existingUser = await _userRepository
            .GetByEmployeeIdAsync(employee.Id, false);

        var uniqueResult = await EnsureUniqueConstraints(
            v,
            ignoreEmployeeId: employee.Id,
            ignoreUserId: existingUser?.Id
        );

        if (uniqueResult.IsFailure)
            return Result<EmployeeWithUserResponse>.Fail(
                uniqueResult.Errors.ToArray()
            );

        var oldSnapshot = BuildSnapshot(employee, existingUser);

        employee.Reactivate();
        employee.Update(v.Name, v.Email, v.Phone);

        if (existingUser is null)
        {
            existingUser = new User(
                v.UserName,
                _passwordHasher.Hash(v.Password.Value),
                v.Role,
                employee.Id
            );

            employee.AssignUserId(existingUser.Id);

            await _userRepository.AddAsync(existingUser);
        }
        else
        {
            existingUser.Reactive();
            existingUser.ChangeUserName(v.UserName);
            existingUser.ChangeRole(v.Role);
            existingUser.ChangePassword(v.Password, _passwordHasher);
            _userRepository.Update(existingUser);
        }

        await _userRepository.SaveChangesAsync();
        await _employeeRepository.SaveChangesAsync();

        await LogAudit(
            currentUserId,
            v,
            AuditAction.UPDATE,
            employee,
            oldSnapshot
        );

        return Success(employee, existingUser);
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
                Employee = new
                {
                    Name = v.Name.Value,
                    CPF = v.Cpf.Value,
                    Email = v.Email.Value,
                    Phone = v.Phone.Value
                },
                User = new
                {
                    UserName = v.UserName.Value,
                    v.Role
                }
            }
        );
    }

    private static Result<EmployeeWithUserResponse> Success(
        EmployeeEntity employee,
        User user)
    {
        return Result<EmployeeWithUserResponse>.Success(
            new EmployeeWithUserResponse(
                user.Adapt<UserResponse>(),
                employee.Adapt<EmployeeResponse>()
            )
        );
    }

    private static object BuildSnapshot(
        EmployeeEntity employee,
        User? user)
    {
        return new
        {
            Employee = new
            {
                Name = employee.Name.Value,
                CPF = employee.CPF.Value,
                Email = employee.Email.Value,
                Phone = employee.Phone.Value,
                employee.IsActive
            },
            User = user is null
                ? null
                : new
                {
                    UserName = user.UserName.Value,
                    user.Role,
                    user.IsActive
                }
        };
    }
}