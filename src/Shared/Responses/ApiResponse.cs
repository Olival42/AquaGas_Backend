namespace AquaGas.Shared.Responses;

using System;

/// <summary>
/// Envelope padrão de resposta da API AquaGas.
/// </summary>
/// <typeparam name="T">Tipo do payload de sucesso.</typeparam>
/// <param name="Success">Indica se a operação foi bem-sucedida.</param>
/// <param name="Data">Dados retornados em caso de sucesso.</param>
/// <param name="Error">Detalhes do erro em caso de falha.</param>
/// <param name="Timestamp">Data/hora UTC da resposta.</param>
public record ApiResponse<T>(bool Success, T? Data, ErrorResponse? Error, DateTimeOffset Timestamp)
{
    /// <summary>Cria resposta de sucesso.</summary>
    public static ApiResponse<T> Ok(T data) =>
        new(true, data, null, DateTimeOffset.UtcNow);

    /// <summary>Cria resposta de falha.</summary>
    public static ApiResponse<T> Fail(ErrorResponse error) =>
       new(false, default, error, DateTimeOffset.UtcNow);
}