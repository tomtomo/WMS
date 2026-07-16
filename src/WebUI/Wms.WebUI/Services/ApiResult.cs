namespace Wms.WebUI.Services;

// Bungkus hasil API agar halaman tidak perlu mengulang try/catch untuk menangani sukses dan gagal.
public sealed class ApiResult<T>
{
    private ApiResult(bool success, T? value, string? errorMessage)
    {
        Success = success;
        Value = value;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }

    public T? Value { get; }

    public string? ErrorMessage { get; }

    public static ApiResult<T> Ok(T value) => new(true, value, null);

    public static ApiResult<T> Fail(string errorMessage) => new(false, default, errorMessage);
}

// Hasil operasi tanpa data balik, misalnya create, update, atau deactivate.
public sealed class ApiResult
{
    private ApiResult(bool success, string? errorMessage)
    {
        Success = success;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }

    public string? ErrorMessage { get; }

    public static ApiResult Ok() => new(true, null);

    public static ApiResult Fail(string errorMessage) => new(false, errorMessage);
}
