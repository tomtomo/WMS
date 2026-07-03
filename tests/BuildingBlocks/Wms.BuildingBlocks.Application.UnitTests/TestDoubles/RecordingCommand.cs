using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.BuildingBlocks.Application.UnitTests.TestDoubles;

// Command untuk PipelineOrderTests — dijalankan lewat pipeline nyata via IMediator.
public sealed record RecordingCommand(int Value) : ICommand<int>;
