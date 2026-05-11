using AquaGas.Auth.Application.Services;
using AquaGas.Customer.Domain.Repositories;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Application.Services;

namespace AquaGas.Customer.Application.UseCases;

public class DeactiveCustomer : IDeactiveCustomer
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserContextService _userContextService;
    private readonly IAuditLogService _auditLogService;

    public DeactiveCustomer(
    ICustomerRepository customerRepository,
    IUserContextService userContextService,
    IAuditLogService auditLogService)
    {
        _customerRepository = customerRepository;
        _userContextService = userContextService;
        _auditLogService = auditLogService;
    }

    public async Task<Result<object>> Execute(Guid id)
    {
        var currentUser = _userContextService.GetUserId();

        var userName = _userContextService.GetUserName();

        if (currentUser.IsFailure)
            return Result<object>.Fail(currentUser.Errors.ToArray());

        if (userName.IsFailure)
            return Result<object>.Fail(userName.Errors.ToArray());

        var customer = await _customerRepository.GetByIdAsync(id);

        if (customer is null)
            return Result<object>.Fail(Error.NotFound("Customer not found"));

        var oldValues = new { customer.Id, customer.IsActive };

        customer.Deactive();

        _customerRepository.Update(customer);
        await _customerRepository.SaveChangesAsync();

        await _auditLogService.LogAsync(
            userId: currentUser.Value,
            userName: userName.Value,
            action: AuditAction.DEACTIVATE,
            entityType: "Customer",
            entityId: customer.Id,
            oldValues: oldValues,
            newValues: new { customer.Id, customer.IsActive }
        );

        return Result<object>.Success(null!);
    }
}