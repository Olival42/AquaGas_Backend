using System.Text.Json.Serialization;

using AquaGas.Shared.Serialization;

namespace AquaGas.Sale.Application.Dtos.Requests;

public record RegisterSaleInput
{
    [JsonConverter(typeof(NullableGuidJsonConverter))]
    public Guid? CustomerId { get; init; }

    public double? Discount { get; init; }

    public List<SaleItemsInput> SaleItems { get; init; } = null!;
}
