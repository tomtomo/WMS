using AwesomeAssertions;
using Microsoft.Extensions.Http.Resilience;
using Wms.BuildingBlocks.Infrastructure.Resilience;
using Xunit;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests;

// Test resilience defaults
public sealed class ResilienceDefaultsTests
{
    [Fact]
    public void Http_resilience_uses_a_short_total_request_timeout()
    {
        var options = new HttpStandardResilienceOptions();

        ResiliencePipelineDefaults.ConfigureHttp(options);

        options.TotalRequestTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        options.AttemptTimeout.Timeout.Should().BeLessThanOrEqualTo(options.TotalRequestTimeout.Timeout);
    }

    [Fact]
    public void Grpc_resilience_uses_a_longer_total_request_timeout()
    {
        var options = new HttpStandardResilienceOptions();

        ResiliencePipelineDefaults.ConfigureGrpc(options);

        options.TotalRequestTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Standard_handler_retains_retry_and_circuit_breaker_strategies()
    {
        var options = new HttpStandardResilienceOptions();

        ResiliencePipelineDefaults.ConfigureHttp(options);

        options.Retry.MaxRetryAttempts.Should().BeGreaterThan(0);
        options.CircuitBreaker.SamplingDuration.Should()
            .BeGreaterThanOrEqualTo(options.AttemptTimeout.Timeout * 2);
    }
}
