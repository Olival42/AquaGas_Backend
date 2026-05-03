namespace AquaGas.Api.Modules.Customer.Application.Dtos.Requests;

public record RegisterAddressInput
{
    public string Street { get; init; } = null!;
    public string Neighborhood { get; init;} = null!;
    public string Number { get; init; } = null!;
    public string? Complement { get; init; }
    public string City { get; init; } = null!;
    public string Cep { get; init; } = null!;
}