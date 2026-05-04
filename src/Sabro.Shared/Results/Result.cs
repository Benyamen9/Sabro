namespace Sabro.Shared.Results;

public readonly record struct Result<T>(bool IsSuccess, T? Value, Error? Error)
{
    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(Error error) => new(false, default, error);
}

public sealed record Error(
    string Code,
    string Message,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? Fields = null)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string what) => new("not_found", $"{what} was not found.");

    public static Error Validation(string message) => new("validation", message);

    public static Error Validation(IReadOnlyDictionary<string, IReadOnlyList<string>> fields) =>
        new("validation", "One or more fields failed validation.", fields);

    public static Error Conflict(string message) => new("conflict", message);
}
