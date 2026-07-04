namespace Wms.Inventory.Infrastructure;

// Placeholder konfigurasi penempatan receiving (default lokasi & assignee putaway).
public sealed class InventoryReceivingOptions
{
    public const string SectionName = "Inventory:Receiving";

    public Guid ReceivingLocationId { get; set; }

    public Guid QuarantineLocationId { get; set; }

    public Guid PutawayDestinationId { get; set; }

    public Guid PutawayAssignee { get; set; }
}
