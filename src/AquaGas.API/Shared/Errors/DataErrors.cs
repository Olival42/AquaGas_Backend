namespace AquaGas.Api.Shared.Errors;

public record DataErrors(string Field, List<string> Message) { }