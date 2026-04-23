using AquaGas.Api.Shared.Results;
using AquaGas.Api.Shared.Errors;

namespace AquaGas.Api.Shared.Responses;

public static class ApiResponseMapper
{
    public static ApiResponse<T> ToApiResponse<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return ApiResponse<T>.Ok(result.Value!);

        var errors = result.Errors ?? new();

        var grouped = errors
            .Where(e => e.Code == "VALIDATION_ERROR")
            .GroupBy(e => e.Field ?? "General")
            .Select(g => new DataErrors(
                g.Key,
                g.Select(e => e.Message).ToList()
            ))
            .ToList();

        if (grouped.Any())
        {
            return ApiResponse<T>.Fail(
                new ErrorResponse(
                    "VALIDATION_ERROR",
                    "Validation failed",
                    grouped
                )
            );
        }

        var first = errors.FirstOrDefault();

        return ApiResponse<T>.Fail(
            new ErrorResponse(
                first?.Code ?? "UNKNOWN",
                first?.Message ?? "Unknown error"
            )
        );
    }
}