namespace AquaGas.Shared.Responses;

using System;

public record ApiResponse<T>(bool Success, T? Data, ErrorResponse? Error, DateTimeOffset Timestamp)
{
    public static ApiResponse<T> Ok(T data) =>
        new(true, data, null, DateTimeOffset.UtcNow);

    public static ApiResponse<T> Fail(ErrorResponse error) =>
       new(false, default, error, DateTimeOffset.UtcNow);
}