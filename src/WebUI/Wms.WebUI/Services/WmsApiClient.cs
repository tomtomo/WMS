using Wms.WebUI.Services.Apis;

namespace Wms.WebUI.Services;

// Penghubung typed client yang menyatukan akses API tiap modul dalam satu entry point.
public sealed class WmsApiClient(
    MasterDataApi masterData,
    ReportingApi reporting,
    OutboundApi outbound,
    InboundApi inbound,
    InventoryApi inventory,
    NotificationsApi notifications)
{
    public MasterDataApi MasterData { get; } = masterData;

    public ReportingApi Reporting { get; } = reporting;

    public OutboundApi Outbound { get; } = outbound;

    public InboundApi Inbound { get; } = inbound;

    public InventoryApi Inventory { get; } = inventory;

    public NotificationsApi Notifications { get; } = notifications;
}
