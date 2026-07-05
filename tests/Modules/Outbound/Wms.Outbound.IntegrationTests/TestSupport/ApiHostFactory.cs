using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Outbound.Api.Endpoints;
using Wms.Outbound.Api.GrpcServices;

namespace Wms.Outbound.IntegrationTests.TestSupport;

// Host in memory setara host Local: kernel Web (ProblemDetails/versioning/correlation), gRPC, endpoint modul.
internal static class ApiHostFactory
{
    public static async Task<WebApplication> StartAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        OutboundTestHost.AddOutboundComposition(builder.Services, connectionString);
        builder.Services.AddSingleton<IApiIdempotencyStore>(new InMemoryIdempotencyStore());
        builder.Services.AddWebBuildingBlocks();
        builder.Services.AddGrpcWebBuildingBlocks();

        var app = builder.Build();
        app.UseWebBuildingBlocks();
        app.MapEndpoints(typeof(CreateWaveEndpoint).Assembly);
        app.MapGrpcService<OutboundReadGrpcService>();

        await app.StartAsync();
        return app;
    }
}
