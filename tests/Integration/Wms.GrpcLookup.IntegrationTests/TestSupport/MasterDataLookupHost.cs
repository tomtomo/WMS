using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.MasterData.Api.GrpcServices;
using Wms.MasterData.Application;
using Wms.MasterData.Infrastructure;
using Wms.MasterData.Infrastructure.Seed;
using Wms.Platform.Local.Cache;

namespace Wms.GrpcLookup.IntegrationTests.TestSupport;

// Menjalankan host gRPC MasterData untuk test, lalu migrate dan seed data referensi. Dipakai sebagai target lookup oleh adapter core.
internal static class MasterDataLookupHost
{
    public static async Task<WebApplication> StartAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:wms"] = connectionString,
        });

        builder.Services.AddApplicationBuildingBlocks(typeof(MasterDataPermissions).Assembly);
        builder.Services.AddBuildingBlocksInfrastructure("wms-grpclookup-masterdata");
        builder.Services.AddMasterDataModule(builder.Configuration);
        builder.Services.AddSystemCurrentUser();
        builder.Services.AddSingleton<ICacheStore, InMemoryCacheStore>();
        builder.Services.AddWebBuildingBlocks();
        builder.Services.AddGrpcWebBuildingBlocks();

        var app = builder.Build();
        app.UseWebBuildingBlocks();
        app.MapGrpcService<MasterDataLookupService>();
        await app.StartAsync();

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MasterDataDbContext>();
        await context.Database.MigrateAsync();
        await MasterDataSeeder.SeedAsync(context);
        return app;
    }
}
