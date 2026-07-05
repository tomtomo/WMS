namespace Wms.Outbound.Application.Abstractions;

// Kebijakan assign operator ke PickingTask.
public interface IPickAssignmentPolicy
{
    Guid AssignPicker(Guid warehouseId);
}
