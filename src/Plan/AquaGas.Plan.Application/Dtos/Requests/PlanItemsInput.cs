using AquaGas.Shared.OpenApi;

namespace AquaGas.Plan.Application.Dtos.Requests;

/// <summary>
/// Item de um plano de assinatura.
/// </summary>
public record PlanItemsInput
{
    /// <summary>Identificador do produto.</summary>
    [OpenApiField(Format = "uuid")]
    public Guid ProductId { get; init; }

    /// <summary>Quantidade por ciclo de entrega.</summary>
    [OpenApiField(Example = "1", Minimum = 1)]
    public int Quantity { get; init; }
}
