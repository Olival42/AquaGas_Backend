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
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContextService _userContextService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UpdateEmployee(
        IEmployeeRepository employeeRepository,
        IAuditLogService auditLogService,
        IUserContextService userContextService,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IUserRepository userRepository)
    {
        _employeeRepository = employeeRepository;
        _auditLogService = auditLogService;
        _userContextService = userContextService;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _userRepository = userRepository;
    }

    public async Task<Result<EmployeeWithUserResponse>> Execute(UpdateEmployeeInput data, Guid id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);

        if (employee is null)
            return Result<EmployeeWithUserResponse>.Fail(
                Error.NotFound("Employee not found"));

        var validation = UpdateEmployeeValidationFactory.Combine(data);

        if (validation.IsFailure)
            return Result<EmployeeWithUserResponse>.Fail(validation.Errors.ToArray());

        var currentUser = _userContextService.GetUserId();
        if (currentUser.IsFailure)
            return Result<EmployeeWithUserResponse>.Fail(currentUser.Errors.ToArray());

        var userNameCtx = _userContextService.GetUserName();
        if (userNameCtx.IsFailure)
            return Result<EmployeeWithUserResponse>.Fail(userNameCtx.Errors.ToArray());

        var v = validation.Value!;

        if (employee.User is null)
            return Result<EmployeeWithUserResponse>.Fail(
                Error.Validation("Employee has no associated user"));

        if (v.UserName is not null)
        {
            var exists = await _userRepository.AnyByUserNameAsync(
                v.UserName.Value,
                employee.User.Id
            );

            if (exists)
                return Result<EmployeeWithUserResponse>.Fail(
                    Error.Conflict("User name already registered"));
        }

        if (v.Email is not null)
        {
            var emailExists = await _employeeRepository.AnyByEmailAsync(
                v.Email.Value,
                employee.Id
            );

            if (emailExists)
                return Result<EmployeeWithUserResponse>.Fail(
                    Error.Conflict("Email already registered"));
        }

        var oldValues = new
        {
            Name = employee.Name.Value,
            Email = employee.Email.Value,
            Phone = employee.Phone.Value,
            UserName = employee.User.UserName.Value,
            Role = employee.User.Role
        };

        employee.Update(v.Name, v.Email, v.Phone);

        var usernameChanged = v.UserName is not null &&
                              employee.User.ChangeUserName(v.UserName);

        var roleChanged = employee.User.ChangeRole(v.Role);

        var passwordChanged = v.Password is not null &&
                              employee.User.ChangePassword(v.Password, _passwordHasher);

        if (usernameChanged || roleChanged || passwordChanged)
            await _refreshTokenRepository.RevokeAllByUserId(employee.User.Id);

        _employeeRepository.Update(employee);
        await _employeeRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            userId: currentUser.Value,
            userName: userNameCtx.Value,
            action: AuditAction.UPDATE,
            entityType: "Employee",
            entityId: employee.Id,
            oldValues: oldValues,
            newValues: new
            {
                Name = employee.Name.Value,
                Email = employee.Email.Value,
                Phone = employee.Phone.Value,
                UserName = employee.User.UserName.Value,
                Role = employee.User.Role,
                PasswordChanged = passwordChanged
            }
        );

        return Result<EmployeeWithUserResponse>.Success(
            new EmployeeWithUserResponse(
                employee.User.Adapt<UserResponse>(),
                employee.Adapt<EmployeeResponse>()
            )
        );
    }
}