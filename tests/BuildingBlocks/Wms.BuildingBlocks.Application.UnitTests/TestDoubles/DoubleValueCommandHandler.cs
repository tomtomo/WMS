using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Application.UnitTests.TestDoubles;

// Handler sample: untuk DoubleValueCommand.
public sealed class DoubleValueCommandHandler : ICommandHandler<DoubleValueCommand, int>
{
    public Task<Result<int>> Handle(DoubleValueCommand request, CancellationToken cancellationToken)
        => Task.FromResult(Result.Success(request.Value * 2));
}
