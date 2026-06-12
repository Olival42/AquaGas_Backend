using AquaGas.Shared.OpenApi;

namespace AquaGas.Plan.Application.Dtos.Requests;

/// <summary>
/// Dados para cadastro de um plano de assinatura (gás/água).
/// </summary>
public record RegisterPlanInput
{
    /// <summary>Identificador do cliente titular.</summary>
    [OpenApiField(Format = "uuid")]
    public Guid CustomerId { get; init; }

    /// <summary>Ciclo de cobrança.</summary>
    [OpenApiField(
        Example = "Monthly",
        AllowedValues = ["Monthly", "Quarterly", "Annual", "Custom"])]
    public string Cycle { get; init; } = null!;

    /// <summary>Percentual de desconto (opcional).</summary>
    [OpenApiField(Example = "0", Minimum = 0.01, Maximum = 100)]
    public double? Discount { get; init; }

    /// <summary>Dia do mês para entrega (1-31).</summary>
    [OpenApiField(Example = "10", Minimum = 1, Maximum = 31)]
    public int DeliveryDay { get; init; }

    /// <summary>Dia do mês para cobrança (1-31).</summary>
    [OpenApiField(Example = "5", Minimum = 1, Maximum = 31)]
    public int BillingDay { get; init; }

    /// <summary>Duração em meses — obrigatório se <c>cycle</c> = Custom (2-60).</summary>
    [OpenApiField(Example = "12", Minimum = 2, Maximum = 60)]
    public int? DurationInMonths { get; init; }

    /// <summary>Ignora avisos não bloqueantes na validação.</summary>
    [OpenApiField(Example = "false")]
    public bool IgnoreWarnings { get; init; }

    /// <summary>Itens do plano (produtos distintos, quantidade &gt; 0).</summary>
    public List<PlanItemsInput> Items { get; init; } = null!;
}
