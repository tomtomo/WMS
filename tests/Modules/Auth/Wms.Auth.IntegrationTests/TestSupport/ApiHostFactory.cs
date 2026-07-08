using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.Auth.Api.Endpoints;
using Wms.Auth.Api.GrpcServices;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Web;

namespace Wms.Auth.IntegrationTests.TestSupport;

// Membuat API host untuk integration test.
internal static class ApiHostFactory
{
    public static async Task<WebApplication> StartAsync(
        string connectionString, TimeProvider? timeProvider = null, bool enableAuthorization = false)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(AuthTestHost.ConfigValues(connectionString));

        AuthTestHost.AddAuthComposition(builder.Services, builder.Configuration, timeProvider);
        builder.Services.AddWebBuildingBlocks();
        builder.Services.AddGrpcWebBuildingBlocks();

        // Konfigurasi autentikasi JWT.
        builder.Services.AddJwtBearerRs256(builder.Configuration);
        if (enableAuthorization)
        {
            // AuthZ penuh: deny-by-default (anon→401); enforcement permission via command-pipeline.
            builder.Services.AddPermissionAuthorization();
            builder.Services.Configure<AuthorizationOptions>(options =>
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        }

        builder.Services.AddSingleton<IApiIdempotencyStore, InMemoryApiIdempotencyStore>();

        var app = builder.Build();
        app.UseWebBuildingBlocks();
        app.UseAuthentication();
        if (enableAuthorization)
        {
            app.UseIsActiveUserCheck();
            app.UseAuthorization();
        }

        app.MapEndpoints(typeof(AuthEndpoints).Assembly);
        app.MapGrpcService<AuthLookupService>();

        await app.StartAsync();
        return app;
    }
}
