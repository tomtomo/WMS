using Wms.Outbound.Application.Abstractions;

namespace Wms.Choreography.IntegrationTests.TestSupport;

internal sealed class FakePickAssignmentPolicy : IPickAssignmentPolicy
{
    public static readonly Guid Picker = Guid.Parse("0b1e5a10-0000-0000-0000-0000000000aa");

    public Guid AssignPicker(Guid warehouseId) => Picker;
}
