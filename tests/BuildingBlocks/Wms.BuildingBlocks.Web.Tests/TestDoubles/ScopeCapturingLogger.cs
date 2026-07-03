using Microsoft.Extensions.Logging;

namespace Wms.BuildingBlocks.Web.Tests.TestDoubles;

// Logger penangkap scope untuk verifikasi state yang dibaca OTel IncludeScopes (correlation-id).
public sealed class ScopeCapturingLogger<T> : ILogger<T>
{
    public List<object?> Scopes { get; } = [];

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        Scopes.Add(state);
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
    }
}

internal sealed class NullScope : IDisposable
{
    public static readonly NullScope Instance = new();

    private NullScope()
    {
    }

    public void Dispose()
    {
    }
}
