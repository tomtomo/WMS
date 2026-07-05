namespace Wms.Outbound.Domain.Enums;

// Lifecycle wave: grouping order yang diproses dan didispatch bersama.
public enum WaveStatus
{
    Active,
    Ready,
    Dispatched,
    Cancelled,
}
