using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Inbound.Api.Endpoints;
using Wms.Inbound.Api.GrpcServices;

namespace Wms.Inbound.IntegrationTests.TestSupport;

// Host in memory setara host Local: kernel Web (ProblemDetails/versioning/correlation), gRPC,
// endpoint modul via MapEndpoints — test komposisi AddInboundModule end to end.
internal static class ApiHostFactory
{
    public static async Task<WebApplication> StartAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        InboundTestHost.AddInboundComposition(builder.Services, connectionString);
        builder.Services.AddSingleton<IApiIdempotencyStore>(new InMemoryIdempotencyStore());
        builder.Services.AddWebBuildingBlocks();
        builder.Services.AddGrpcWebBuildingBlocks();

        var app = builder.Build();
        app.UseWebBuildingBlocks();
        app.MapEndpoints(typeof(CreateGoodsReceiptEndpoint).Assembly);
        app.MapGrpcService<GoodsReceiptGrpcService>();

        await app.StartAsync();
        return app;
    }
}
