namespace AquaGas.Api.Modules.Customer.Application.Dtos.Requests;

public record UpdateCustomerInput
{
    public string? Name { get; init; }
    public string? Document { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }

    public UpdateAddressInput? Address { get; init; }
}