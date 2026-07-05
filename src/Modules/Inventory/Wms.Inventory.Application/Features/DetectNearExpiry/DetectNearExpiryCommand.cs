using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Inventory.Application.Features.DetectNearExpiry;

// scan Stock mendekati kadaluarsa
public sealed record DetectNearExpiryCommand(int ThresholdDays) : ICommand;
