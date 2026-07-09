using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.MasterData.Api.Endpoints;
using Wms.MasterData.Api.GrpcServices;

namespace Wms.MasterData.IntegrationTests.TestSupport;

// Host in memory setara host Local: kernel Web (ProblemDetails/versioning/correlation), gRPC, REST endpoint modul.
internal static class ApiHostFactory
{
    public static async Task<WebApplication> StartAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        MasterDataTestHost.AddMasterDataComposition(builder.Services, connectionString);
        builder.Services.AddWebBuildingBlocks();
        builder.Services.AddGrpcWebBuildingBlocks();

        // Test cukup pakai store in memory untuk idempotency key. Adapter Postgres dipasang di host lewat AddLocalPlatform.
        builder.Services.AddSingleton<IApiIdempotencyStore, InMemoryApiIdempotencyStore>();

        var app = builder.Build();
        app.UseWebBuildingBlocks();
        app.MapEndpoints(typeof(WarehouseEndpoints).Assembly);
        app.MapGrpcService<MasterDataLookupService>();

        await app.StartAsync();
        return app;
    }
}
