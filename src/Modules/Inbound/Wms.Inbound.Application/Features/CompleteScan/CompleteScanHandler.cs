using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Application.EventTranslation;

namespace Wms.Inbound.Application.Features.CompleteScan;

internal sealed class CompleteScanHandler(
    IGoodsReceiptRepository repository,
    GoodsReceiptEventTranslator translator) : ICommandHandler<CompleteScanCommand>
{
    public async Task<Result> Handle(CompleteScanCommand command, CancellationToken cancellationToken)
    {
        var loaded = await GoodsReceiptLoader.LoadAsync(repository, command.GoodsReceiptId, cancellationToken);
        if (loaded.IsFailure)
        {
            return loaded;
        }

        var completed = loaded.Value.CompleteScan();
        if (completed.IsFailure)
        {
            return completed;
        }

        // GoodsReceiptPendingReviewRaised ke Outbox — satu transaksi dengan state.
        await translator.TranslateAndClearAsync(loaded.Value, cancellationToken);
        return completed;
    }
}
