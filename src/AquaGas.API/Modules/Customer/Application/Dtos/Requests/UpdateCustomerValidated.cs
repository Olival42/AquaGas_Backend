namespace AquaGas.Api.Modules.Customer.Application.Dtos.Requests;

using AquaGas.Api.Modules.Customer.Domain.ValueObjects.Customer;
using AquaGas.Api.Shared.Domain.ValueObjects;

public record UpdateCustomerValidated()
{
    public CustomerName? Name { get; set; }
    public Document? Document { get; set; }
    public Email? Email { get; set; }
    public Phone? Phone { get; set; }

    public UpdateAddressValidated? Address { get; set; }
}