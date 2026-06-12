namespace AquaGas.Customer.Application.Dtos.Requests;

using AquaGas.Customer.Domain.ValueObjects.Customer;
using AquaGas.Shared.Domain.ValueObjects;

public record UpdateCustomerValidated()
{
    public CustomerName? Name { get; set; }
    public Document? Document { get; set; }
    public Email? Email { get; set; }
    public Phone? Phone { get; set; }

    public UpdateAddressValidated? Address { get; set; }
}