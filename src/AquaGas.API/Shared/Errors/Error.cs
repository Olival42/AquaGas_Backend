namespace AquaGas.Api.Shared.Errors;

public record Error(string Code, string Message)
{
    public static Error Validation(string message)
        => new("VALIDATION_ERROR", message);

    public static Error Unauthorized(string message)
        => new("UNAUTHORIZED", message);

    public static Error NotFound(string message)
        => new("NOT_FOUND", message);
}