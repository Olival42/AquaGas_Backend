using AquaGas.Shared.OpenApi;

namespace AquaGas.Customer.Application.Dtos.Requests;

/// <summary>
/// Dados para cadastro de um novo cliente.
/// </summary>
public record RegisterCustomerInput
{
    /// <summary>Nome completo ou razão social.</summary>
    [OpenApiField(Example = "Maria Silva", MinLength = 1)]
    public string Name { get; init; } = null!;

    /// <summary>CPF (11 dígitos) ou CNPJ (14 dígitos).</summary>
    [OpenApiField(
        Example = "52998224725",
        Pattern = @"^(\d{11}|\d{14})$",
        Description = "CPF ou CNPJ. Aceita máscara; normalizado para somente números.")]
    public string Document { get; init; } = null!;

    /// <summary>E-mail de contato.</summary>
    [OpenApiField(Example = "maria.silva@email.com", Format = "email")]
    public string Email { get; init; } = null!;

    /// <summary>Telefone com DDD.</summary>
    [OpenApiField(Example = "11999998888", Pattern = @"^\d{10,11}$")]
    public string Phone { get; init; } = null!;

    /// <summary>Endereço principal do cliente.</summary>
    public RegisterAddressInput Address { get; init; } = null!;
}
