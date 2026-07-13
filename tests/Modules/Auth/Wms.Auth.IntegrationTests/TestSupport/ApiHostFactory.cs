using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Wms.Auth.Api.Endpoints;
using Wms.Auth.Api.GrpcServices;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Web;

namespace Wms.Auth.IntegrationTests.TestSupport;

// Membuat API host untuk integration test.
internal static class ApiHostFactory
{
    public static async Task<WebApplication> StartAsync(
        string connectionString,
        TimeProvider? timeProvider = null,
        bool enableAuthorization = false,
        IConfigurationManager<OpenIdConnectConfiguration>? entraConfigurationManager = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var configValues = AuthTestHost.ConfigValues(connectionString);
        if (entraConfigurationManager is not null)
        {
            configValues["Entra:Enabled"] = "true";
            configValues["Entra:TenantId"] = "test-tenant";
            configValues["Entra:ClientId"] = TestEntraTokens.Audience;
        }

        builder.Configuration.AddInMemoryCollection(configValues);

        AuthTestHost.AddAuthComposition(builder.Services, builder.Configuration, timeProvider);
        if (entraConfigurationManager is not null)
        {
            builder.Services.RemoveAll<IConfigurationManager<OpenIdConnectConfiguration>>();
            builder.Services.AddSingleton(entraConfigurationManager);
        }

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
