using AquaGas.Shared.OpenApi;

namespace AquaGas.Employee.Application.Dtos.Requests;

/// <summary>
/// Dados cadastrais do funcionário.
/// </summary>
public record EmployeeInput
{
    /// <summary>Nome do funcionário.</summary>
    [OpenApiField(Example = "João Pereira")]
    public string Name { get; init; } = null!;

    /// <summary>CPF do funcionário (11 dígitos).</summary>
    [OpenApiField(Example = "52998224725", Pattern = @"^\d{11}$")]
    public string Cpf { get; init; } = null!;

    /// <summary>E-mail corporativo.</summary>
    [OpenApiField(Example = "joao@empresa.com", Format = "email")]
    public string Email { get; init; } = null!;

    /// <summary>Telefone de contato.</summary>
    [OpenApiField(Example = "11988887777", Pattern = @"^\d{10,11}$")]
    public string Phone { get; init; } = null!;
}
