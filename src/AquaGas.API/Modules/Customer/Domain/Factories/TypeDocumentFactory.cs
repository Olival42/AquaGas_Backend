namespace AquaGas.Api.Modules.Auth.Domain.Factories;

using AquaGas.Api.Modules.Customer.Domain.Enums;
using AquaGas.Api.Shared.Errors;
using AquaGas.Api.Shared.Results;

public static class TypeDocumentFactory
{
    public static Result<TypeDocument> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result<TypeDocument>.Fail(
                Error.Validation("TypeDocument is required", "TypeDocument")
            );

        var value = input.Trim().ToUpper();

        return value switch
        {
            "PF" => Result<TypeDocument>.Success(TypeDocument.PF),
            "PJ" => Result<TypeDocument>.Success(TypeDocument.PJ),

            _ => Result<TypeDocument>.Fail(
                Error.Validation("Type of document is invalid. Allowed: PF, PJ", "TypeDocument")
            )
        };
    }
}
