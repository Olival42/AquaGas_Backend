namespace AquaGas.API.Shared.Application.Validation;

using AquaGas.Api.Shared.Errors;

public static class StringValidator
{
    public static string? Validate(
        string? input,
        string field,
        int minLength,
        List<Error> errors)
    {
        if (input is null) return null;

        var value = input.Trim();

        if (string.IsNullOrWhiteSpace(value))
            errors.Add(Error.Validation($"{field} cannot be empty", field));
        else if (value.Length < minLength)
            errors.Add(Error.Validation($"{field} must have at least {minLength} characters", field));
        else
            return value;

        return null;
    }
}