using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Xunit;

namespace Wms.BuildingBlocks.Web.Tests;

public sealed class IEndpointDiscoveryTests
{
    [Fact]
    public void MapEndpoints_routes_every_IEndpoint_in_the_assembly()
    {
        var app = WebApplication.CreateBuilder().Build();

        app.MapEndpoints(typeof(IEndpointDiscoveryTests).Assembly);

        var patterns = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToList();

        patterns.Should().Contain(PingEndpoint.Route);
        patterns.Should().Contain(PongEndpoint.Route);
    }

    // dummy: hanya keduanya yang mengimplementasikan IEndpoint di assembly test
    private sealed class PingEndpoint : IEndpoint
    {
        public const string Route = "/_test/discovery/ping";

        public static void MapEndpoint(IEndpointRouteBuilder app)
            => app.MapGet(Route, () => Results.Ok("ping"));
    }

    private sealed class PongEndpoint : IEndpoint
    {
        public const string Route = "/_test/discovery/pong";

        public static void MapEndpoint(IEndpointRouteBuilder app)
            => app.MapGet(Route, () => Results.Ok("pong"));
    }
}
