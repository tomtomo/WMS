using Wms.Outbound.Application.Abstractions;

namespace Wms.Outbound.IntegrationTests.TestSupport;

// Fake assignment — operator tetap.
internal sealed class FakePickAssignmentPolicy : IPickAssignmentPolicy
{
    public static readonly Guid Picker = Guid.Parse("0b1e5a10-0000-0000-0000-0000000000aa");

    public Guid AssignPicker(Guid warehouseId) => Picker;
}
