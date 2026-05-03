namespace AquaGas.Api.Modules.Customer.Application.Dtos.Requests;

using AquaGas.Api.Modules.Customer.Domain.ValueObjects.Customer;
using AquaGas.Api.Shared.Domain.ValueObjects;

public record RegisterCustomerValidated()
{
    public CustomerName Name { get; init; } = null!;
    public Document Document { get; init; } = null!;
    public Email Email { get; init; } = null!;
    public Phone Phone { get; init; } = null!;

    public RegisterAddressValidated Address { get; init; } = null!;
}