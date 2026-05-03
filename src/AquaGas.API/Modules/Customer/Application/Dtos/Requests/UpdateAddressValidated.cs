using AquaGas.Api.Modules.Customer.Domain.ValueObjects.Address;

namespace AquaGas.Api.Modules.Customer.Application.Dtos.Requests;

public record UpdateAddressValidated
{   
    public Guid? Id { get; set; }
    public string? Street { get; set; }
    public string? Neighborhood { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? City { get; set; }
    public Cep? Cep { get; set; }
}