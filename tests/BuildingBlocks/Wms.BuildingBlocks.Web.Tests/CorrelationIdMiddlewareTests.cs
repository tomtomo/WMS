using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Wms.BuildingBlocks.Web;
using Wms.BuildingBlocks.Web.Tests.TestDoubles;
using Xunit;

namespace Wms.BuildingBlocks.Web.Tests;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task Generates_a_guid_and_pushes_it_to_the_log_scope_when_header_absent()
    {
        var logger = new ScopeCapturingLogger<CorrelationIdMiddleware>();
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask, logger);

        await middleware.InvokeAsync(context);

        var id = CorrelationId.Get(context);
        id.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(id, out _).Should().BeTrue();

        var scope = logger.Scopes.Should().ContainSingle()
            .Which.Should().BeAssignableTo<IReadOnlyDictionary<string, object>>().Subject;
        scope[CorrelationId.LogScopeKey].Should().Be(id);
    }

    [Fact]
    public async Task Inbound_header_flows_to_response_header_and_endpoint()
    {
        await using var app = await StartTestHostAsync();
        using var client = app.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(CorrelationId.HeaderName, "abc-123");
        using var response = await client.SendAsync(request);

        response.Headers.GetValues(CorrelationId.HeaderName).Should().ContainSingle().Which.Should().Be("abc-123");
        (await response.Content.ReadAsStringAsync()).Should().Be("abc-123");
    }

    [Fact]
    public async Task Missing_header_is_generated_and_echoed_in_response()
    {
        await using var app = await StartTestHostAsync();
        using var client = app.GetTestClient();

        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));

        var echoed = response.Headers.GetValues(CorrelationId.HeaderName).Single();
        Guid.TryParse(echoed, out _).Should().BeTrue();
        (await response.Content.ReadAsStringAsync()).Should().Be(echoed);
    }

    private static async Task<WebApplication> StartTestHostAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        app.UseCorrelationId();

        app.MapGet("/", (HttpContext ctx) => CorrelationId.Get(ctx));

        await app.StartAsync();
        return app;
    }
}
