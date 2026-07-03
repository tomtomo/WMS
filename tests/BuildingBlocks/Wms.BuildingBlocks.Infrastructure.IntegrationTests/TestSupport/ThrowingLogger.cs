using Microsoft.Extensions.Logging;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestSupport;

internal sealed class ThrowingLogger<TCategory> : ILogger<TCategory>
{
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        throw new InvalidOperationException("telemetry backend down");
}
