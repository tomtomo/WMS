using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Contracts.Abstractions;

namespace Wms.BuildingBlocks.Infrastructure.Eventing;

// Registrasi consumer untuk satu integration event.
public sealed class RailConsumerRegistration
{
    public required string LogicalName { get; init; }

    public required DeliveryClass DeliveryClass { get; init; }

    public required Func<IServiceProvider, MessageEnvelope, CancellationToken, Task<Result>> InvokeAsync { get; init; }
}
