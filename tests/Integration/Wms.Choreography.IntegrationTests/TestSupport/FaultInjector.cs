using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Choreography.IntegrationTests.TestSupport;

// Mensimulasikan keberhasilan atau kegagalan consumer.
internal sealed class FaultInjector
{
    private int _attempts;
    private int _successes;

    public required Func<MessageEnvelope, int, Result> Behavior { get; init; }

    public int Attempts => Volatile.Read(ref _attempts);

    public int Successes => Volatile.Read(ref _successes);

    public Task<Result> InvokeAsync(IServiceProvider serviceProvider, MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        var attempt = Interlocked.Increment(ref _attempts);
        var result = Behavior(envelope, attempt);
        if (result.IsSuccess)
        {
            Interlocked.Increment(ref _successes);
        }

        return Task.FromResult(result);
    }
}
