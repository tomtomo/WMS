using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Xunit;

namespace Wms.Reporting.IntegrationTests;

// Pastikan feature flag Reporting.TelemetrySummary membaca status aktif, nonaktif, dan tidak tersedia dengan benar.
// Jika flag tidak ditemukan, endpoint tetap nonaktif secara default.
public sealed class TelemetryFeatureFlagTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public async Task Flag_resolves_from_configuration(string configured, bool expected)
    {
        var manager = BuildFeatureManager(new()
        {
            [$"FeatureManagement:{ReportingFeatureFlags.TelemetrySummary}"] = configured,
        });

        (await manager.IsEnabledAsync(ReportingFeatureFlags.TelemetrySummary)).Should().Be(expected);
    }

    [Fact]
    public async Task Flag_absent_defaults_to_disabled()
    {
        var manager = BuildFeatureManager([]);

        (await manager.IsEnabledAsync(ReportingFeatureFlags.TelemetrySummary)).Should().BeFalse();
    }

    private static IFeatureManager BuildFeatureManager(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddFeatureManagement();
        return services.BuildServiceProvider().GetRequiredService<IFeatureManager>();
    }
}
