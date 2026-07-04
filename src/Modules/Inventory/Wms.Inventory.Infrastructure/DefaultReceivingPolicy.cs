using Microsoft.Extensions.Options;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Infrastructure;

// Impl placeholder IReceivingPolicy dari konfigurasi.
internal sealed class DefaultReceivingPolicy(IOptions<InventoryReceivingOptions> options) : IReceivingPolicy
{
    private readonly InventoryReceivingOptions _options = options.Value;

    public LocationId ReceivingLocation(Guid warehouseId) => Resolve(_options.ReceivingLocationId, "receiving");

    public LocationId QuarantineLocation(Guid warehouseId) => Resolve(_options.QuarantineLocationId, "quarantine");

    public PutawaySuggestion SuggestPutaway(Sku sku, Guid warehouseId) =>
        new(Resolve(_options.PutawayDestinationId, "putaway destination"), _options.PutawayAssignee);

    private static LocationId Resolve(Guid locationId, string role)
    {
        var result = LocationId.Create(locationId);
        return result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException(
                $"InventoryReceivingOptions: lokasi '{role}' belum dikonfigurasi (section '{InventoryReceivingOptions.SectionName}').");
    }
}
