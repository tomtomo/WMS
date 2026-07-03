using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestSupport;
using Wms.BuildingBlocks.Infrastructure.Telemetry;
using Xunit;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests;

// Test TelemetrySink
public sealed class TelemetrySinkTests
{
    [Fact]
    public async Task Record_swallows_backend_failures_and_stays_fail_open()
    {
        var sink = new TelemetrySink(new ThrowingLogger<TelemetrySink>());

        var record = async () => await sink.RecordAsync(
            "inbound.gr_confirmed",
            new Dictionary<string, string> { ["module"] = "inbound" });

        await record.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Record_completes_on_the_happy_path()
    {
        var sink = new TelemetrySink(NullLogger<TelemetrySink>.Instance);

        var record = async () => await sink.RecordAsync("inbound.gr_confirmed", new Dictionary<string, string>());

        await record.Should().NotThrowAsync();
    }
}
