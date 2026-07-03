using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Application.UnitTests.TestDoubles;

// Handler yang mencatat dirinya sebagai bagian terakhir pipeline.
public sealed class RecordingCommandHandler(PipelineRecorder recorder) : ICommandHandler<RecordingCommand, int>
{
    public Task<Result<int>> Handle(RecordingCommand request, CancellationToken cancellationToken)
    {
        recorder.Add("Handler");
        return Task.FromResult(Result.Success(request.Value));
    }
}
