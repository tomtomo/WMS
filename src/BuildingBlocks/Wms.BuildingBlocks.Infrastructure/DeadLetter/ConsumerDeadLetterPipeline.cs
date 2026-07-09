using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Infrastructure.DeadLetter;

// Retry manual dengan MaxAttempts dan delay, lalu dead letter.
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
                    await deadLetterStore.StoreAsync(logicalName, payload, ex.Message, attempt, cancellationToken);
                    return;
                }

                await Task.Delay(_retryDelay, timeProvider, cancellationToken);
            }
        }
    }

    // Perlakukan Result failure seperti exception agar tetap masuk mekanisme retry.
    public async Task ExecuteAsync(
        string logicalName,
        string payload,
        Func<CancellationToken, Task<Result>> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var failureReason = await RunAttemptAsync(handler, cancellationToken);
            if (failureReason is null)
            {
                return;
            }

            if (attempt == MaxAttempts)
            {
                await deadLetterStore.StoreAsync(logicalName, payload, failureReason, attempt, cancellationToken);
                return;
            }

            await Task.Delay(_retryDelay, timeProvider, cancellationToken);
        }
    }

    // Jalankan satu percobaan dan kembalikan alasan kegagalan jika ada.
    private static async Task<string?> RunAttemptAsync(
        Func<CancellationToken, Task<Result>> handler, CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler(cancellationToken);
            return result.IsSuccess ? null : result.Error.Code;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ex.Message;
        }
    }
}
