using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.BuildingBlocks.Infrastructure.DeadLetter;

// Retry manual dengan MaxAttempts dan delay, lalu dead-letter. Bukan Polly, loop retry DLQ terpisah dari resilience pipeline sinkron.
public sealed class ConsumerDeadLetterPipeline(
    IDeadLetterStore deadLetterStore,
    TimeProvider timeProvider,
    TimeSpan? retryDelay = null)
{
    public const int MaxAttempts = 3;

    private static readonly TimeSpan _defaultRetryDelay = TimeSpan.FromMilliseconds(200);

    private readonly TimeSpan _retryDelay = retryDelay ?? _defaultRetryDelay;

    public async Task ExecuteAsync(
        string logicalName,
        string payload,
        Func<CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await handler(cancellationToken);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Batas retry tercapai, lalu dead letter.
                if (attempt == MaxAttempts)
                {
                    await deadLetterStore.StoreAsync(logicalName, payload, ex.Message, cancellationToken);
                    return;
                }

                await Task.Delay(_retryDelay, timeProvider, cancellationToken);
            }
        }
    }
}
