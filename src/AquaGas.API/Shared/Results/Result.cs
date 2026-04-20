using AquaGas.Api.Shared.Errors;

namespace AquaGas.Api.Shared.Results;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public List<Error> Errors { get; }

    public bool IsValidationError { get; }

    protected Result(bool success, List<Error>? errors = null)
    {
        IsSuccess = success;
        Errors = errors ?? new();

        IsValidationError = Errors.Any(e => e.Code == "VALIDATION_ERROR");
    }

    public static Result Success() => new(true);

    public static Result Fail(params Error[] errors)
        => new(false, errors.ToList());
}