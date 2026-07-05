using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Outbound.Application.Features.DispatchWave;

// SPV mendispatch wave siap (Ready jadi Dispatched). Emit ShipmentDispatched, lalu tutup order
// (terpenuhi semua jadi Closed, backorder outstanding balik ke backlog).
[RequiresPermission(OutboundPermissions.DispatchWave)]
public sealed record DispatchWaveCommand(Guid WaveId) : ICommand;
