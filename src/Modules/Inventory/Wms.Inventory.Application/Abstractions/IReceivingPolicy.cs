using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Application.Abstractions;

// Kebijakan penempatan saat receiving: lokasi receiving/quarantine, saran putaway.
public interface IReceivingPolicy
{
    // Lokasi landing balance OnHand (line Good) di warehouse.
    LocationId ReceivingLocation(Guid warehouseId);

    // Lokasi karantina balance (line QcHold) di warehouse.
    LocationId QuarantineLocation(Guid warehouseId);

    // Saran putaway untuk balance OnHand.
    PutawaySuggestion SuggestPutaway(Sku sku, Guid warehouseId);
}

// Saran putaway: rak tujuan, operator terassign.
public sealed record PutawaySuggestion(LocationId Destination, Guid AssignedTo);
