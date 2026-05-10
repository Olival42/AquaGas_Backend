using AquaGas.Shared.Errors;

namespace AquaGas.Shared.Results;

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool success, T? value, List<Error>? errors)
        : base(success, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value)
        => new(true, value, null);

    public static new Result<T> Fail(params Error[] errors)
        => new(false, default, errors.ToList());
}