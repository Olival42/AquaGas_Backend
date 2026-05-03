namespace AquaGas.Api.Modules.Customer.Application.Dtos.Requests;

public record UpdateAddressInput
{
    public Guid? AddressId { get; init; }
    public string? Street { get; init; }
    public string? Neighborhood { get; init;}
    public string? Number { get; init; }
    public string? Complement { get; init; }
    public string? City { get; init; }
    public string? Cep { get; init; }
}