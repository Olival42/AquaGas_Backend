using CustomerEntity = AquaGas.Customer.Domain.Models.Customer;
using AquaGas.Customer.Domain.Enums;
using AquaGas.Customer.Domain.Repositories;
using Mapster;
using AquaGas.Application.Services;
using AquaGas.Shared.Results;
using AquaGas.Customer.Application.Dtos.Responses;
using AquaGas.Customer.Application.Dtos.Requests;
using AquaGas.Shared.Errors;
using AquaGas.Customer.Domain.Models;
using AquaGas.Shared.Domain.Enums;
using AquaGas.Customer.Application.Services;
using AquaGas.Auth.Application.Services;

namespace AquaGas.Customer.Application.UseCases;

public class UpdateCustomer : IUpdateCustomer
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContextService _userContextService;

    public UpdateCustomer(
        ICustomerRepository customerRepository,
        IAuditLogService auditLogService,
        IUserContextService userContextService)
    {
        _customerRepository = customerRepository;
        _auditLogService = auditLogService;
        _userContextService = userContextService;
    }

    public async Task<Result<CustomerResponse>> Execute(UpdateCustomerInput data, Guid id)
    {
        var validation = UpdateCustomerValidationFactory.Combine(data);
        if (validation.IsFailure)
            return Result<CustomerResponse>.Fail(validation.Errors.ToArray());

        var currentUser = _userContextService.GetUserId();
        var userName = _userContextService.GetUserName();

        if (currentUser.IsFailure)
            return Result<CustomerResponse>.Fail(currentUser.Errors.ToArray());

        if (userName.IsFailure)
            return Result<CustomerResponse>.Fail(userName.Errors.ToArray());

        var customer = await _customerRepository.GetByIdAsync(id);

        if (customer is null)
            return Result<CustomerResponse>.Fail(
                Error.NotFound("Customer not found"));

        var v = validation.Value!;

        if (v.Document is not null)
        {
            var exists = await _customerRepository.AnyByDocumentAsync(
                v.Document.Value,
                customer.Id
            );

            if (exists)
                return Result<CustomerResponse>.Fail(
                    Error.Conflict(v.Document.Type == TypeDocument.PF
                        ? "CPF already registered"
                        : "CNPJ already registered")
                );
        }

        Address? address = null;

        if (v.Address is not null)
        {
            address = customer.Addresses
                .FirstOrDefault(a => a.Id == v.Address.Id);

            if (address is null)
                return Result<CustomerResponse>.Fail(
                    Error.NotFound("Address not found"));
        }

        var oldValues = BuildSnapshot(customer);

        customer.Update(v.Name, v.Document, v.Email, v.Phone);

        if (v.Address is not null && address is not null)
            address.Update(
                v.Address.Street,
                v.Address.Neighborhood,
                v.Address.Number,
                v.Address.Complement,
                v.Address.City,
                v.Address.Cep
            );

        _customerRepository.Update(customer);
        await _customerRepository.SaveChangesAsync();

        var newValues = BuildSnapshot(customer);

        await _auditLogService.LogAsync(
            userId: currentUser.Value,
            userName: userName.Value,
            action: AuditAction.UPDATE,
            entityType: "Customer",
            entityId: customer.Id,
            oldValues: oldValues,
            newValues: newValues
        );

        return Result<CustomerResponse>.Success(
            customer.Adapt<CustomerResponse>()
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