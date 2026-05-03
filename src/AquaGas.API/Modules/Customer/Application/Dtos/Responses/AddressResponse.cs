namespace AquaGas.Api.Modules.Customer.Application.Dtos.Responses;

public record AddressResponse
{
    public Guid Id { get; init; }
    public string Street { get; init; } = null!;
    public string Neighborhood { get; init; } = null!;
    public string Number { get; init; } = null!;
    public string? Complement { get; init; } = null!;
    public string City { get; init; } = null!;
    public string Cep { get; init; } = null!;
}