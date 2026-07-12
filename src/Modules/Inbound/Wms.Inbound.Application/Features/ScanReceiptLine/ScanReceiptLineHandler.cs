using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Domain.ValueObjects;

namespace Wms.Inbound.Application.Features.ScanReceiptLine;

internal sealed class ScanReceiptLineHandler(
    IGoodsReceiptRepository repository,
    IOperationalTelemetryEmitter telemetry,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<ScanReceiptLineCommand>
{
    public async Task<Result> Handle(ScanReceiptLineCommand command, CancellationToken cancellationToken)
    {
        var loaded = await GoodsReceiptLoader.LoadAsync(repository, command.GoodsReceiptId, cancellationToken);
        if (loaded.IsFailure)
        {
            return loaded;
        }

        var line = ScannedLine.Create(command.Sku, command.ActualQty, command.Batch, command.Expiry, command.LineStatus);
        if (line.IsFailure)
        {
            return line;
        }

        var scanned = loaded.Value.Scan(line.Value);
        if (scanned.IsSuccess)
        {
            // Gunakan current user sebagai operator jika ID nya berupa GUID, dan SYSTEM atau anonymous disimpan sebagai null.
            await telemetry.EmitAsync(
                new OperationalTelemetryRecord(
                    timeProvider.GetUtcNow(),
                    loaded.Value.WarehouseId,
                    Guid.TryParse(currentUser.UserId, out var operatorId) ? operatorId : null,
                    OperationalTelemetryEventType.ScanCompleted,
                    command.GoodsReceiptId,
                    command.ActualQty),
                cancellationToken);
        }

        return scanned;
    }
}
