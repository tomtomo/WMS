namespace Wms.Notifications.Consumers;

// Role bawaan untuk subscription notifikasi.
internal static class NotificationRoles
{
    public static readonly Guid Supervisor = new("a1a1a1a1-0000-0000-0000-000000000001");

    public static readonly Guid Purchasing = new("a1a1a1a1-0000-0000-0000-000000000002");

    public static readonly Guid InventoryPlanner = new("a1a1a1a1-0000-0000-0000-000000000003");
}
