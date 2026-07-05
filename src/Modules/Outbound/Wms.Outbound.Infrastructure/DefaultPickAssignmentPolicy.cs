using Microsoft.Extensions.Options;
using Wms.Outbound.Application.Abstractions;

namespace Wms.Outbound.Infrastructure;

internal sealed class DefaultPickAssignmentPolicy(IOptions<OutboundPickingOptions> options) : IPickAssignmentPolicy
{
    private readonly OutboundPickingOptions _options = options.Value;

    public Guid AssignPicker(Guid warehouseId) => _options.DefaultPickerId != Guid.Empty
        ? _options.DefaultPickerId
        : throw new InvalidOperationException(
            $"OutboundPickingOptions: DefaultPickerId belum dikonfigurasi (section '{OutboundPickingOptions.SectionName}').");
}
