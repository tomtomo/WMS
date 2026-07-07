using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.MasterData.Infrastructure;

namespace Wms.CrossCutting.IntegrationTests.TestSupport;

// Host REST MasterData in-memory untuk bukti full chain xmin ke HTTP 409.
internal static class MasterDataApiHost
{
    public static async Task<WebApplication> StartAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:wms"] = connectionString,
        });

        builder.Services.AddApplicationBuildingBlocks(typeof(Wms.MasterData.Application.MasterDataPermissions).Assembly);
        builder.Services.AddBuildingBlocksInfrastructure("wms-cc-masterdata-api");
        builder.Services.AddMasterDataModule(builder.Configuration);
        builder.Services.AddSingleton<ICacheStore, Wms.Platform.Local.Cache.InMemoryCacheStore>();
        builder.Services.AddSingleton<ConflictInjector>();
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ConflictInjectionBehavior<,>));
        builder.Services.AddWebBuildingBlocks();

        var app = builder.Build();
        app.UseWebBuildingBlocks();
        app.MapEndpoints(typeof(Wms.MasterData.Api.Endpoints.WarehouseEndpoints).Assembly);
        await app.StartAsync();

        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<MasterDataDbContext>().Database.MigrateAsync();
        return app;
    }
}
