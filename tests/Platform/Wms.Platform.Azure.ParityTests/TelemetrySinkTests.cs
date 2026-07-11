using System.Diagnostics;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Wms.Platform.Azure.Telemetry;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Telemetry tetap berjalan meski backend tidak tersedia, yang berubah hanya exporternya.
public sealed class TelemetrySinkTests
{
    private static readonly IReadOnlyDictionary<string, string> _tags =
        new Dictionary<string, string> { ["module"] = "inbound", ["outcome"] = "confirmed" };

    [Fact]
    public async Task Recorded_telemetry_becomes_an_activity_with_its_tags()
    {
        using var activitySource = new ActivitySource($"wms-test-{Guid.NewGuid():N}");
        var recorded = new List<Activity>();
        using var listener = ListenTo(activitySource, recorded);
        var sink = new AppInsightsTelemetrySink(activitySource, NullLogger<AppInsightsTelemetrySink>.Instance);

        await sink.RecordAsync("gr.confirmed", _tags);

        var activity = recorded.Should().ContainSingle().Subject;
        activity.OperationName.Should().Be("gr.confirmed");
        activity.GetTagItem("module").Should().Be("inbound");
        activity.GetTagItem("outcome").Should().Be("confirmed");
    }

    [Fact]
    public async Task Sink_failure_never_reaches_the_business_operation()
    {
        using var activitySource = new ActivitySource($"wms-test-{Guid.NewGuid():N}");
        var logger = Substitute.For<ILogger<AppInsightsTelemetrySink>>();
        logger.BeginScope(Arg.Any<IReadOnlyDictionary<string, string>>())
            .Throws(new InvalidOperationException("exporter App Insights mati"));
        var sink = new AppInsightsTelemetrySink(activitySource, logger);

        var record = () => sink.RecordAsync("gr.confirmed", _tags);

        await record.Should().NotThrowAsync("kontrak ITelemetrySink fail-open");
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
}
