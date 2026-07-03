using System.Net;
using Asp.Versioning;
using Asp.Versioning.Builder;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Wms.BuildingBlocks.Web.Tests;

public sealed class ApiVersioningTests
{
    [Fact]
    public async Task Endpoint_is_reachable_at_the_v1_url_segment()
    {
        await using var app = await StartVersionedHostAsync();
        using var client = app.GetTestClient();

        using var response = await client.GetAsync(new Uri("/v1/ping", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("pong");
    }

    [Fact]
    public async Task Unknown_version_is_rejected()
    {
        await using var app = await StartVersionedHostAsync();
        using var client = app.GetTestClient();

        using var response = await client.GetAsync(new Uri("/v2/ping", UriKind.Relative));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    private static async Task<WebApplication> StartVersionedHostAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddApiVersioningDefaults();
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        var versionSet = app.NewApiVersionSet().HasApiVersion(new ApiVersion(1, 0)).ReportApiVersions().Build();
        app.MapGet("/v{version:apiVersion}/ping", () => "pong")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(new ApiVersion(1, 0));

        await app.StartAsync();
        return app;
    }
}
