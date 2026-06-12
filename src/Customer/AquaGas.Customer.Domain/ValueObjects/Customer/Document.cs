
using System.Text.RegularExpressions;
using AquaGas.Customer.Domain.Enums;
using AquaGas.Shared.Domain.ValueObjects;
using AquaGas.Shared.Errors;
using AquaGas.Shared.Results;

namespace AquaGas.Customer.Domain.ValueObjects.Customer;

public sealed class Document
{
    public string Value { get; }
    public TypeDocument Type { get; }

    private Document(string value, TypeDocument type)
    {
        Value = value;
        Type = type;
    }

    public static Result<Document> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Document>.Fail(
                Error.Validation("Document is required", "Document")
            );

        var normalized = Regex.Replace(value, @"\D", "");

        if (normalized.Length == 11)
        {
            var cpf = Cpf.Create(normalized);
            if (cpf.IsFailure) return Result<Document>.Fail(cpf.Errors.ToArray());

            return Result<Document>.Success(
                new Document(normalized, TypeDocument.PF)
            );
        }

        if (normalized.Length == 14)
        {
            var cnpj = Cnpj.Create(normalized);
            if (cnpj.IsFailure) return Result<Document>.Fail(cnpj.Errors.ToArray());

            return Result<Document>.Success(
                new Document(normalized, TypeDocument.PJ)
            );
        }

        return Result<Document>.Fail(
            Error.Validation("Invalid document", "Document")
        );
    }
}