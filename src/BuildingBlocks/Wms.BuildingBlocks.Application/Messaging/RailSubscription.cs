using Wms.Contracts.Abstractions;

namespace Wms.BuildingBlocks.Application.Messaging;

// Subscription modul terhadap integration event tertentu.
public sealed record RailSubscription(string LogicalName, DeliveryClass DeliveryClass);
