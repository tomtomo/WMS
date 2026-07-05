namespace Wms.Outbound.Infrastructure;

// Konfigurasi placeholder assignment picking
public sealed class OutboundPickingOptions
{
    public const string SectionName = "Outbound:Picking";

    public Guid DefaultPickerId { get; set; }
}
