using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Wms.Platform.Hosting.Tests;

// /health agregat semua check.
public sealed class HealthEndpointTests : IAsyncLifetime
{
    private WebApplication? _app;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.AddServiceDefaults();

        builder.Services.AddHealthChecks()
            .AddCheck("sick-dependency", () => HealthCheckResult.Unhealthy("down"));

        _app = builder.Build();
        _app.MapDefaultEndpoints();
        await _app.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task Alive_returns_200_even_when_a_dependency_check_is_unhealthy()
    {
        using var client = _app!.GetTestClient();

        using var response = await client.GetAsync(new Uri("/alive", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_aggregates_all_checks_and_reports_unhealthy_dependency()
    {
        using var client = _app!.GetTestClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Health_returns_200_when_only_default_self_check_registered()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.AddServiceDefaults();
        await using var app = builder.Build();
        app.MapDefaultEndpoints();
        await app.StartAsync();
        using var client = app.GetTestClient();

        using var health = await client.GetAsync(new Uri("/health", UriKind.Relative));
        using var alive = await client.GetAsync(new Uri("/alive", UriKind.Relative));

        health.StatusCode.Should().Be(HttpStatusCode.OK);
        alive.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
