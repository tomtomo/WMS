using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Contracts;

namespace Wms.Inventory.Application.Features.AllocateWave;

// Consumer WaveReleased dari Outbound untuk jalur alokasi. Idempotent dua lapis: Inbox guard (eventId, handlerType),
// lalu natural key (waveId, orderId, sku) per line di handler
public sealed class WaveReleasedConsumer(
    IInboxGuard inbox,
    AllocateWaveHandler handler,
    IUnitOfWork unitOfWork)
{
    public const string HandlerType = "AllocateWave";

    public async Task<Result> ConsumeAsync(
        WaveReleased integrationEvent,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        // Lapis 1: redelivery broker at least once (eventId sama) untuk handler ini.
        if (await inbox.HasProcessedAsync(eventId, HandlerType, cancellationToken))
        {
            return Result.Success();
        }

        var handled = await handler.HandleAsync(integrationEvent, cancellationToken);
        if (handled.IsFailure)
        {
            // Tanpa MarkProcessed & tanpa commit, event bisa diretry.
            return handled;
        }

        await inbox.MarkProcessedAsync(eventId, HandlerType, cancellationToken);

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
