using AquaGas.Api.Modules.Customer.Domain.ValueObjects.Address;

namespace AquaGas.Api.Modules.Customer.Application.Dtos.Requests;

public record RegisterAddressValidated
{
    public string Street { get; init; } = null!;
    public string Neighborhood { get; init; } = null!;
    public string Number { get; init; } = null!;
    public string? Complement { get; init; }
    public string City { get; init; } = null!;
    public Cep Cep { get; init; } = null!;
}