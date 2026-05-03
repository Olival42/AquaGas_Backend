using CustomerEntity = AquaGas.Api.Modules.Customer.Domain.Models.Customer;
using AquaGas.Api.Modules.Auth.Application.Services;
using AquaGas.Api.Modules.Customer.Application.Dtos.Requests;
using AquaGas.Api.Modules.Customer.Domain.Repositories;
using AquaGas.Api.Shared.Results;
using AquaGas.API.Modules.Customer.Application.Services;
using AquaGas.API.Shared.Application.Services;
using Mapster;
using AquaGas.Api.Shared.Errors;
using AquaGas.API.Shared.Domain.Enums;
using AquaGas.Api.Modules.Customer.Domain.Enums;
using AquaGas.Api.Modules.Customer.Domain.Models;

namespace AquaGas.Api.Modules.Customer.Application.UseCases;

public class RegisterCustomer : IRegisterCustomer
{
    private readonly ICustomerRepository _repository;
    private readonly IAuditLogService _audit;
    private readonly IUserContextService _userContext;

    public RegisterCustomer(
        ICustomerRepository repository,
        IAuditLogService audit,
        IUserContextService userContext)
    {
        _repository = repository;
        _audit = audit;
        _userContext = userContext;
    }

    public async Task<Result<CustomerResponse>> Execute(RegisterCustomerInput data)
    {
        var validation = RegisterCustomerValidationFactory.Combine(data);
        if (validation.IsFailure)
            return Result<CustomerResponse>.Fail(validation.Errors.ToArray());

        var userId = _userContext.GetUserId();
        var userName = _userContext.GetUserName();

        if (userId.IsFailure)
            return Result<CustomerResponse>.Fail(userId.Errors.ToArray());

        if (userName.IsFailure)
            return Result<CustomerResponse>.Fail(userName.Errors.ToArray());

        var v = validation.Value!;

        var customer = await _repository.GetByDocumentAsync(v.Document.Value);

        if (customer is null)
            return await Create(v, userId.Value, userName.Value!);

        if (customer.IsActive)
        {
            return Result<CustomerResponse>.Fail(
                Error.Conflict(
                    v.Document.Type == TypeDocument.PF
                        ? "CPF already registered"
                        : "CNPJ already registered"
                )
            );
        }

        return await Reactivate(customer, v, userId.Value, userName.Value!);
    }

    private async Task<Result<CustomerResponse>> Create(
        RegisterCustomerValidated v,
        Guid userId,
        string userName)
    {
        var customer = v.Adapt<CustomerEntity>();

        customer.AddAddress(CreateAddress(customer.Id, v));

        await _repository.AddAsync(customer);
        await _repository.SaveChangesAsync();

        await Log(userId, userName, customer, null, AuditAction.CREATE);

        return Result<CustomerResponse>.Success(customer.Adapt<CustomerResponse>());
    }

    private async Task<Result<CustomerResponse>> Reactivate(
        CustomerEntity customer,
        RegisterCustomerValidated v,
        Guid userId,
        string userName)
    {
        var oldSnapshot = BuildSnapshot(customer);

        customer.Reactivate();
        customer.Update(v.Name, v.Document, v.Email, v.Phone);

        customer.ReplaceAddresses(CreateAddress(customer.Id, v));

        await _repository.SaveChangesAsync();

        await Log(userId, userName, customer, oldSnapshot, AuditAction.UPDATE);

        return Result<CustomerResponse>.Success(customer.Adapt<CustomerResponse>());
    }

    private static Address CreateAddress(Guid customerId, RegisterCustomerValidated v)
        => new Address(
            customerId,
            v.Address.Street,
            v.Address.Neighborhood,
            v.Address.Number,
            v.Address.Complement,
            v.Address.City,
            v.Address.Cep
        );

    private async Task Log(
        Guid userId,
        string userName,
        CustomerEntity customer,
        object? oldValues,
        AuditAction action)
    {
        await _audit.LogAsync(
            userId,
            userName,
            action,
            "Customer",
            customer.Id,
            oldValues,
            BuildSnapshot(customer)
        );
    }

    private static object BuildSnapshot(CustomerEntity c)
    {
        return new
        {
            c.Name,
            Document = c.Document.Value,
            c.Document.Type,
            Email = c.Email.Value,
            Phone = c.Phone.Value,
            c.IsActive,
            Addresses = c.Addresses.Select(a => new
            {
                a.Street,
                a.Number,
                a.City,
                Cep = a.Cep.Value
            }).ToList()
        };
    }
}