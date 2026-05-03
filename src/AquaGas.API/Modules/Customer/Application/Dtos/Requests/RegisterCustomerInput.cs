namespace AquaGas.Api.Modules.Customer.Application.Dtos.Requests;

public record RegisterCustomerInput
{
    public string Name { get; init; } = null!;
    public string Document { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string Phone { get; init; } = null!;

    public RegisterAddressInput Address { get; init; } = null!;
}