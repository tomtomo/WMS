using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.IntegrationTests.TestSupport;

internal sealed class FakeReceivingPolicy : IReceivingPolicy
{
    public static readonly Guid ReceivingLocationId = Guid.Parse("aa000000-0000-0000-0000-000000000001");

    public static readonly Guid QuarantineLocationId = Guid.Parse("aa000000-0000-0000-0000-000000000002");

    public static readonly Guid PutawayDestinationId = Guid.Parse("aa000000-0000-0000-0000-000000000003");

    public static readonly Guid PutawayAssignee = Guid.Parse("aa000000-0000-0000-0000-000000000004");

    public LocationId ReceivingLocation(Guid warehouseId) => LocationId.Create(ReceivingLocationId).Value;

    public LocationId QuarantineLocation(Guid warehouseId) => LocationId.Create(QuarantineLocationId).Value;

    public PutawaySuggestion SuggestPutaway(Sku sku, Guid warehouseId) =>
        new(LocationId.Create(PutawayDestinationId).Value, PutawayAssignee);
}
