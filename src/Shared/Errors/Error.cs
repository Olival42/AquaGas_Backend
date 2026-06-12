namespace AquaGas.Shared.Errors;

public record Error(string Code, string Message, string? Field = null)
{
    public static Error Validation(string message, string? field = null)
        => new("VALIDATION_ERROR", message, field);

    public static Error Conflict(string message)
        => new("CONFLICT", message);

    public static Error InsufficientStock()
        => new("INSUFFICIENT_STOCK", "Insufficient stock");

    public static Error InsufficientStockForProduct(
        string productName,
        Guid productId,
        int available,
        int requested)
        => new(
            "INSUFFICIENT_STOCK",
            $"Insufficient stock for product '{productName}' (Id: {productId}). Available: {available}, requested: {requested}.");

    public static Error Unauthorized(string message)
        => new("UNAUTHORIZED", message);

    public static Error NotFound(string message)
        => new("NOT_FOUND", message);

    public static Error Forbidden(string message)
        => new("FORBIDDEN", message);
}