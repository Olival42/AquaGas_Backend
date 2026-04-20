namespace AquaGas.Api.Shared.Responses;

public record ErrorResponse(
    string Code,
    string Message,
    List<string>? Details = null
);