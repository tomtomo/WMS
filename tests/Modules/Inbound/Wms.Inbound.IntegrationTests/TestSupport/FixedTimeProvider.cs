namespace Wms.Inbound.IntegrationTests.TestSupport;

// Clock untuk timestamp.
internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
