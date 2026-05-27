namespace AquaGas.Shared.OpenApi;

/// <summary>
/// Metadados de formato para documentação OpenAPI (Swagger) de uma propriedade.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class OpenApiFieldAttribute : Attribute
{
    /// <summary>Descrição detalhada exibida no schema.</summary>
    public string? Description { get; init; }

    /// <summary>Valor de exemplo.</summary>
    public string? Example { get; init; }

    /// <summary>Expressão regular aceita (pattern OpenAPI).</summary>
    public string? Pattern { get; init; }

    /// <summary>Valores permitidos para campos textuais (enum).</summary>
    public string[]? AllowedValues { get; init; }

    /// <summary>Formato OpenAPI (ex.: uuid, date-time, email).</summary>
    public string? Format { get; init; }

    /// <summary>Valor mínimo numérico (-1 = não definido).</summary>
    public double Minimum { get; init; } = -1;

    /// <summary>Valor máximo numérico (-1 = não definido).</summary>
    public double Maximum { get; init; } = -1;

    /// <summary>Tamanho mínimo de string (-1 = não definido).</summary>
    public int MinLength { get; init; } = -1;

    /// <summary>Tamanho máximo de string (-1 = não definido).</summary>
    public int MaxLength { get; init; } = -1;
}
