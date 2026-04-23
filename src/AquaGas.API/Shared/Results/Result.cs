using AquaGas.Api.Shared.Errors;

namespace AquaGas.Api.Shared.Results;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public List<Error> Errors { get; }

    public bool IsValidationError => Errors.Any(e => e.Code == "VALIDATION_ERROR");

    protected Result(bool success, List<Error>? errors = null)
    {
        IsSuccess = success;
        Errors = errors ?? new();
    }

    public static Result Success() => new(true);

    public static Result Fail(params Error[] errors)
        => new(false, errors.ToList());

    public static Result Combine(params Result[] results)
    {
        var errors = results
            .Where(r => r.IsFailure)
            .SelectMany(r => r.Errors)
            .ToList();

        if (errors.Any())
        {
            var criticalError = errors.FirstOrDefault(e => e.Code != "VALIDATION_ERROR");
            if (criticalError != null)
                return Fail(criticalError);

            return new Result(false, errors);
        }

        return Success();
    }
}