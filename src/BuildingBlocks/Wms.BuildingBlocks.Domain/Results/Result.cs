namespace Wms.BuildingBlocks.Domain.Results;

// Result pattern — kegagalan proses bisnis dibawa sebagai value, bukan exception.
public class Result
{
    protected Result(bool isSuccess, ResultErrorType errorType, Error error)
    {
        IsSuccess = isSuccess;
        ErrorType = errorType;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public ResultErrorType ErrorType { get; }

    public Error Error { get; }

    public static Result Success() => new(true, ResultErrorType.None, Error.None);

    public static Result<TValue> Success<TValue>(TValue value)
        => new(value, true, ResultErrorType.None, Error.None);

    public static Result Failure(Error error) => new(false, ResultErrorType.Failure, error);

    public static Result<TValue> Failure<TValue>(Error error)
        => new(default!, false, ResultErrorType.Failure, error);

    public static Result Invalid(Error error) => new(false, ResultErrorType.Validation, error);

    public static Result<TValue> Invalid<TValue>(Error error)
        => new(default!, false, ResultErrorType.Validation, error);

    public static Result Conflict(Error error) => new(false, ResultErrorType.Conflict, error);

    public static Result<TValue> Conflict<TValue>(Error error)
        => new(default!, false, ResultErrorType.Conflict, error);

    public static Result NotFound(Error error) => new(false, ResultErrorType.NotFound, error);

    public static Result<TValue> NotFound<TValue>(Error error)
        => new(default!, false, ResultErrorType.NotFound, error);

    public static Result Forbidden(Error error) => new(false, ResultErrorType.Forbidden, error);

    public static Result<TValue> Forbidden<TValue>(Error error)
        => new(default!, false, ResultErrorType.Forbidden, error);
}

// Result<T> membawa nilai di jalur sukses.
public sealed class Result<TValue> : Result
{
    private readonly TValue _value;

    internal Result(TValue value, bool isSuccess, ResultErrorType errorType, Error error)
        : base(isSuccess, errorType, error)
        => _value = value;

    // Akses Value saat gagal, bukan jalur kegagalan proses bisnis.
    public TValue Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("Tidak boleh mengakses Value pada Result gagal.");

    public Result<TNext> Map<TNext>(Func<TValue, TNext> map)
        => IsSuccess ? Success(map(_value)) : new Result<TNext>(default!, false, ErrorType, Error);

    public Result<TNext> Bind<TNext>(Func<TValue, Result<TNext>> bind)
        => IsSuccess ? bind(_value) : new Result<TNext>(default!, false, ErrorType, Error);
}
