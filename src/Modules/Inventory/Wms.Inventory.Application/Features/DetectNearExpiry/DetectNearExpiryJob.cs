using MediatR;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Inventory.Application.Features.DetectNearExpiry;

// Hangfire Local / timer cloud: kirim DetectNearExpiryCommand lewat pipeline.
public sealed class DetectNearExpiryJob(ISender sender, IOptions<InventoryExpiryOptions> options) : IRecurringJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new DetectNearExpiryCommand(options.Value.ThresholdDays), cancellationToken);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Expiry-scan gagal: {result.Error.Code} — {result.Error.Message}");
        }
    }
}
