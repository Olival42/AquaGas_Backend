namespace AquaGas.Shared.Responses;

public record ErrorResponse(
    string Code,
    string Message,
    object? Details = null!
);