using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.Auth.Api.GrpcServices;
using Wms.Auth.Application;
using Wms.Auth.Infrastructure;
using Wms.Auth.Infrastructure.Seed;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Platform.Shared.Security;

namespace Wms.GrpcLookup.IntegrationTests.TestSupport;

// Menjalankan host gRPC Auth untuk test, lalu migrate dan seed data awal. Dipakai sebagai target UserDirectory di Notifications.
internal static class AuthLookupHost
{
    public static async Task<WebApplication> StartAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:wms"] = connectionString,
        });

        builder.Services.AddApplicationBuildingBlocks(typeof(AuthPermissions).Assembly);
        builder.Services.AddBuildingBlocksInfrastructure("wms-grpclookup-auth");
        builder.Services.AddAuthModule(builder.Configuration);
        builder.Services.AddSystemCurrentUser();
        builder.Services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        builder.Services.AddWebBuildingBlocks();
        builder.Services.AddGrpcWebBuildingBlocks();

        var app = builder.Build();
        app.UseWebBuildingBlocks();
        app.MapGrpcService<AuthLookupService>();
        await app.StartAsync();

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await context.Database.MigrateAsync();
        await AuthSeeder.SeedAsync(context, scope.ServiceProvider.GetRequiredService<IPasswordHasher>());
        return app;
    }
}
