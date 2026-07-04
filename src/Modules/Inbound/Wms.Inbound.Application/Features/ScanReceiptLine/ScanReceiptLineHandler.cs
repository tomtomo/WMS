using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Domain.ValueObjects;

namespace Wms.Inbound.Application.Features.ScanReceiptLine;

internal sealed class ScanReceiptLineHandler(IGoodsReceiptRepository repository)
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

        return loaded.Value.Scan(line.Value);
    }
}
