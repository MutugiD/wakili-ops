namespace WakiliDms.Core.Common;

public sealed record Result(bool Succeeded, string? Error)
{
    public static Result Ok() => new(true, null);

    public static Result Fail(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Error message is required.", nameof(error));
        }

        return new Result(false, error);
    }
}

public sealed record Result<T>(bool Succeeded, T? Value, string? Error)
{
    public static Result<T> Ok(T value) => new(true, value, null);

    public static Result<T> Fail(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Error message is required.", nameof(error));
        }

        return new Result<T>(false, default, error);
    }
}
