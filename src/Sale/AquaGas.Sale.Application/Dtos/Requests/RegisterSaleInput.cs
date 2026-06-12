using System.Text.Json.Serialization;
using AquaGas.Shared.OpenApi;
using AquaGas.Shared.Serialization;

namespace AquaGas.Sale.Application.Dtos.Requests;

/// <summary>
/// Dados para registro de uma venda avulsa.
/// </summary>
public record RegisterSaleInput
{
    /// <summary>Identificador do cliente (opcional).</summary>
    [OpenApiField(Format = "uuid")]
    [JsonConverter(typeof(NullableGuidJsonConverter))]
    public Guid? CustomerId { get; init; }

    /// <summary>Percentual de desconto (opcional, 0,01 a 100).</summary>
    [OpenApiField(Example = "5", Minimum = 0.01, Maximum = 100)]
    public double? Discount { get; init; }

    /// <summary>Itens da venda (ao menos um).</summary>
    public List<SaleItemsInput> SaleItems { get; init; } = null!;
}
