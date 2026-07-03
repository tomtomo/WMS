using System.Net;
using Asp.Versioning;
using Asp.Versioning.Builder;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Wms.BuildingBlocks.Web.Tests;

public sealed class ScalarOpenApiSmokeTests
{
    [Fact]
    public async Task OpenApi_document_and_scalar_ui_are_reachable_in_development()
    {
        await using var app = await StartDocHostAsync(Environments.Development);
        using var client = app.GetTestClient();

        using var openApi = await client.GetAsync(new Uri("/swagger/v1/swagger.json", UriKind.Relative));
        using var scalar = await client.GetAsync(new Uri("/scalar/v1", UriKind.Relative));

        openApi.StatusCode.Should().Be(HttpStatusCode.OK);
        (await openApi.Content.ReadAsStringAsync()).Should().Contain("openapi");
        scalar.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenApi_document_is_not_exposed_outside_development()
    {
        await using var app = await StartDocHostAsync(Environments.Production);
        using var client = app.GetTestClient();

        using var openApi = await client.GetAsync(new Uri("/swagger/v1/swagger.json", UriKind.Relative));

        openApi.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<WebApplication> StartDocHostAsync(string environment)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.Services.AddApiVersioningDefaults();
        builder.Services.AddOpenApiDocumentation();
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        var versionSet = app.NewApiVersionSet().HasApiVersion(new ApiVersion(1, 0)).Build();
        app.MapGet("/v{version:apiVersion}/ping", () => "pong")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(new ApiVersion(1, 0));
        app.MapOpenApiDocumentation();

        await app.StartAsync();
        return app;
    }
}
