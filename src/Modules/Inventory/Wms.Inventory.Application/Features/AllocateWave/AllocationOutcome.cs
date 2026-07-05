using Wms.Inventory.Contracts.Enums;

namespace Wms.Inventory.Application.Features.AllocateWave;

// Outcome eksplisit satu wave
internal static class AllocationOutcome
{
    public static AllocationStatus Resolve(int allocationCount, int shortfallCount)
    {
        if (allocationCount == 0)
        {
            return AllocationStatus.Unfulfilled;
        }

        return shortfallCount == 0 ? AllocationStatus.FullyAllocated : AllocationStatus.PartiallyAllocated;
    }
}
