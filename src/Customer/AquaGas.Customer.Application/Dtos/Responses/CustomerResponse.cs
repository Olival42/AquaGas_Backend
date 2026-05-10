namespace AquaGas.Customer.Application.Dtos.Responses;

public record CustomerResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Document { get; init; } = null!;
    public string TypeDocument { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string Phone { get; init; } = null!;

    public AddressResponse Address { get; init; } = null!;
}