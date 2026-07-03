using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.BuildingBlocks.Application.UnitTests.TestDoubles;

// Command sample untuk test ICommand<T> resolve lewat IMediator.
public sealed record DoubleValueCommand(int Value) : ICommand<int>;
