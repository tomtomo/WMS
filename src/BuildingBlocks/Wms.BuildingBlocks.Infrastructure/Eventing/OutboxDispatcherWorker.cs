using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Infrastructure.Outbox;

namespace Wms.BuildingBlocks.Infrastructure.Eventing;

// Worker untuk memproses outbox event.
public sealed class OutboxDispatcherWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcherWorker> logger) : BackgroundService
{
    // Jalankan satu siklus dispatch outbox.
    internal async Task DrainOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
        var deadLetterStore = scope.ServiceProvider.GetRequiredService<IDeadLetterStore>();

        var pending = await dbContext.Set<OutboxRecord>()
            .Where(row => row.ProcessedAt == null)
            .OrderBy(row => row.OccurredAt)
            .Take(OutboxDispatcher.BatchSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var row in pending)
        {
            // Rekonstruksi envelope dari row
            var envelope = new MessageEnvelope(
                row.Id,
                row.LogicalName,
                row.DeliveryClass,
                row.OccurredAt,
                row.Payload,
                row.Traceparent,
                row.Tracestate);
            try
            {
                await dispatcher.DispatchAsync(envelope, cancellationToken).ConfigureAwait(false);
                row.ProcessedAt = timeProvider.GetUtcNow();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
#pragma warning disable S2221 // Gagal publish satu row tidak boleh gagalkan batch
            catch (Exception exception)
#pragma warning restore S2221
            {
                row.AttemptCount++;
                if (row.AttemptCount >= OutboxDispatcher.MaxPublishAttempts)
                {
                    // Tandai row selesai sebelum dicatat ke dead letter agar tidak diproses ulang saat restart.
                    row.ProcessedAt = timeProvider.GetUtcNow();
                    await deadLetterStore.StoreAsync(row.LogicalName, row.Payload, exception.Message, row.AttemptCount, cancellationToken).ConfigureAwait(false);
                    logger.LogError(
                        exception,
                        "Dispatch outbox {LogicalName} (event {EventId}) gagal {Attempt}× → dead_letter.",
                        row.LogicalName,
                        row.Id,
                        row.AttemptCount);
                }
                else
                {
                    logger.LogWarning(
                        exception,
                        "Dispatch outbox {LogicalName} (event {EventId}) gagal; row tetap Pending (attempt {Attempt}).",
                        row.LogicalName,
                        row.Id,
                        row.AttemptCount);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DrainOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable S2221 // Loop worker tidak boleh mati karena satu siklus poll gagal.
            catch (Exception exception)
#pragma warning restore S2221
            {
                logger.LogError(exception, "OutboxDispatcherWorker: siklus poll gagal; lanjut poll berikut.");
            }

            try
            {
                await Task.Delay(OutboxDispatcher.PollInterval, timeProvider, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
