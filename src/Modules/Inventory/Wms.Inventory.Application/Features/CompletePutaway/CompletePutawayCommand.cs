using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Inventory.Application.Features.CompletePutaway;

// Operator menuntaskan putaway: PutawayTask Assigned ke Completed, Stock OnHand ke Available (pindah ke rak).
[RequiresPermission(InventoryPermissions.CompletePutaway)]
public sealed record CompletePutawayCommand(Guid PutawayTaskId, Guid ActualDestinationId, Guid? OperatorId) : ICommand;
