namespace AquaGas.Shared.Responses;

/// <summary>
/// Estrutura padronizada de erro retornada pela API.
/// </summary>
/// <param name="Code">Código de erro (ex.: VALIDATION_ERROR, NOT_FOUND).</param>
/// <param name="Message">Mensagem descritiva do erro.</param>
/// <param name="Details">Detalhes adicionais (ex.: campos de validação).</param>
public record ErrorResponse(
    string Code,
    string Message,
    object? Details = null!
);