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

        var currentUser = _userContextService.GetUserId();

        if (currentUser.IsFailure)
            return Result<EmployeeWithUserResponse>.Fail(currentUser.Errors.ToArray());

        var v = validation.Value!;

        var employee = new EmployeeEntity(
            v.Name,
            v.Cpf,
            v.Email,
            v.Phone
        );

        var passwordHash = _passwordHasher.Hash(v.Password.Value);

        var user = new User(
            v.UserName,
            passwordHash,
            v.Role,
            employee.Id
        );

        if (await _employeeRepository.AnyByCPFAsync(employee.CPF.Value))
            return Result<EmployeeWithUserResponse>.Fail(
                Error.Conflict("CPF already registered")
            );

        if (await _employeeRepository.AnyByEmailAsync(employee.Email.Value))
            return Result<EmployeeWithUserResponse>.Fail(
                Error.Conflict("Email already registered")
            );

        if (await _userRepository.AnyByUserNameAsync(user.UserName.Value))
            return Result<EmployeeWithUserResponse>.Fail(
                Error.Conflict("Username already registered")
            );

        var employeeSnapshot = new
        {
            v.Name,
            v.Cpf,
            v.Email,
            v.Phone
        };

        var userSnapshot = new
        {
            v.UserName,
            v.Role
        };

        await _employeeRepository.AddAsync(employee);
        await _userRepository.AddAsync(user);
        await _employeeRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            userId: currentUser.Value,
            userName: v.UserName.Value,
            action: AuditAction.CREATE,
            entityType: "Employee",
            entityId: employee.Id,
            oldValues: null,
            newValues: new
            {
                Employee = employeeSnapshot,
                User = userSnapshot
            }
        );

        var employeeDto = employee.Adapt<EmployeeResponse>();
        var userDto = user.Adapt<UserResponse>();

        return Result<EmployeeWithUserResponse>
            .Success(new EmployeeWithUserResponse(userDto, employeeDto));
    }
}