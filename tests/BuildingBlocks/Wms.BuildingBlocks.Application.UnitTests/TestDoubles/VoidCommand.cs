using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.BuildingBlocks.Application.UnitTests.TestDoubles;

// Command tanpa nilai balik
public sealed record VoidCommand : ICommand;
