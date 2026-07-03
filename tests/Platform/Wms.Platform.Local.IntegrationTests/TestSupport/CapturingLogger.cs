using Microsoft.Extensions.Logging;

namespace Wms.Platform.Local.IntegrationTests.TestSupport;

// Logger test
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = [];

    public List<IReadOnlyList<KeyValuePair<string, object?>>> States { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Messages.Add(formatter(state, exception));
        if (state is IReadOnlyList<KeyValuePair<string, object?>> pairs)
        {
            States.Add(pairs);
        }
    }

    public string? StateValue(string key) =>
        States.SelectMany(pairs => pairs)
            .FirstOrDefault(pair => pair.Key == key)
            .Value?
            .ToString();
}

internal sealed class NullScope : IDisposable
{
    private NullScope()
    {
    }

    public static NullScope Instance { get; } = new();

    public void Dispose()
    {
    }
}
