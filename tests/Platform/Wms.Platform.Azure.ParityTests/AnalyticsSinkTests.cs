using System.Diagnostics;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Wms.Platform.Azure.Telemetry;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Pastikan event analitik dikirim sebagai span OpenTelemetry tanpa mengganggu proses bisnis jika pengiriman gagal.
public sealed class AnalyticsSinkTests
{
    [Fact]
    public async Task Emitted_analytics_event_becomes_an_activity()
    {
        using var activitySource = new ActivitySource($"wms-test-{Guid.NewGuid():N}");
        var recorded = new List<Activity>();
        using var listener = ListenTo(activitySource, recorded);
        var sink = new AppInsightsAnalyticsSink(activitySource, NullLogger<AppInsightsAnalyticsSink>.Instance);

        await sink.EmitAsync(new SampleAggregate("REC-01", 42));

        var activity = recorded.Should().ContainSingle().Subject;
        activity.OperationName.Should().Be("Analytics.SampleAggregate");
        activity.GetTagItem("analytics.event_type").Should().Be("SampleAggregate");
    }

    [Fact]
    public async Task Sink_failure_never_reaches_the_business_operation()
    {
        using var activitySource = new ActivitySource($"wms-test-{Guid.NewGuid():N}");
        var logger = Substitute.For<ILogger<AppInsightsAnalyticsSink>>();
        logger.BeginScope(Arg.Any<Dictionary<string, object>>())
            .Throws(new InvalidOperationException("exporter App Insights mati"));
        var sink = new AppInsightsAnalyticsSink(activitySource, logger);

        var emit = () => sink.EmitAsync(new SampleAggregate("REC-01", 42));

        await emit.Should().NotThrowAsync("kontrak IAnalyticsSink fail-open");
    }

    private static ActivityListener ListenTo(ActivitySource activitySource, List<Activity> recorded)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == activitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = recorded.Add,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private sealed record SampleAggregate(string Location, int Count);
}
