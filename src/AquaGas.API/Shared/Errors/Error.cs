namespace AquaGas.Api.Shared.Errors;

public record Error(string Code, string Message, string? Field = null)
{
    public static Error Validation(string message, string? field = null)
        => new("VALIDATION_ERROR", message, field);

    public static Error Conflict(string message)
        => new("CONFLICT", message);

    public static Error Unauthorized(string message)
        => new("UNAUTHORIZED", message);

    public static Error NotFound(string message)
        => new("NOT_FOUND", message);
}