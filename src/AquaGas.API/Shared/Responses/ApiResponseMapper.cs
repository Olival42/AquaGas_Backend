using AquaGas.Api.Shared.Results;

namespace AquaGas.Api.Shared.Responses;

public static class ApiResponseMapper
{
    public static ApiResponse<T> ToApiResponse<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return ApiResponse<T>.Ok(result.Value!);

        var errors = result.Errors ?? new();

        var validationErrors = errors
            .Where(e => e.Code == "VALIDATION_ERROR")
            .Select(e => e.Message)
            .ToList();

        var error = validationErrors.Any()
            ? new ErrorResponse(
                "VALIDATION_ERROR",
                "Validation failed",
                validationErrors
              )
            : new ErrorResponse(
                errors.FirstOrDefault()?.Code ?? "UNKNOWN",
                errors.FirstOrDefault()?.Message ?? "Unknown error"
            );

        return ApiResponse<T>.Fail(error);
    }
}