using AquaGas.Application.Services;
using AquaGas.Auth.Application.Dtos.Responses;
using AquaGas.Auth.Application.Services;
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

namespace AquaGas.Employee.Application.UseCases;

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

        var user = await _userRepository.GetByEmployeeIdAsync(id);

        if (user is null)
            return Result<EmployeeWithUserResponse>.Fail(
                Error.NotFound("User not found"));

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

        if (employee.UserId == Guid.Empty)
            return Result<EmployeeWithUserResponse>.Fail(
                Error.Validation("Employee has no associated user"));

        if (v.UserName is not null)
        {
            var exists = await _userRepository.AnyByUserNameAsync(
                v.UserName.Value,
                user.Id
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
            UserName = user.UserName.Value,
            Role = user.Role
        };

        employee.Update(v.Name, v.Email, v.Phone);

        var usernameChanged = v.UserName is not null &&
                              user.ChangeUserName(v.UserName);

        var roleChanged = user.ChangeRole(v.Role);

        var passwordChanged = v.Password is not null &&
                              user.ChangePassword(v.Password, _passwordHasher);

        if (usernameChanged || roleChanged || passwordChanged)
            await _refreshTokenRepository.RevokeAllByUserId(user.Id);

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
                UserName = user.UserName.Value,
                Role = user.Role,
                PasswordChanged = passwordChanged
            }
        );

        return Result<EmployeeWithUserResponse>.Success(
            new EmployeeWithUserResponse(
                user.Adapt<UserResponse>(),
                employee.Adapt<EmployeeResponse>()
            )
        );
    }
}