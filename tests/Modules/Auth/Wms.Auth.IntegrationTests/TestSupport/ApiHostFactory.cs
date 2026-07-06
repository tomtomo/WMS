using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.Auth.Api.Endpoints;
using Wms.Auth.Api.GrpcServices;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Auth.IntegrationTests.TestSupport;

// Membuat API host untuk integration test.
internal static class ApiHostFactory
{
    public static async Task<WebApplication> StartAsync(string connectionString, TimeProvider? timeProvider = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(AuthTestHost.ConfigValues(connectionString));

        AuthTestHost.AddAuthComposition(builder.Services, builder.Configuration, timeProvider);
        builder.Services.AddWebBuildingBlocks();
        builder.Services.AddGrpcWebBuildingBlocks();

        // Konfigurasi autentikasi JWT.
        builder.Services.AddJwtBearerRs256(builder.Configuration);
        builder.Services.AddSingleton<IApiIdempotencyStore, InMemoryApiIdempotencyStore>();

        var app = builder.Build();
        app.UseWebBuildingBlocks();
        app.UseAuthentication();
        app.MapEndpoints(typeof(AuthEndpoints).Assembly);
        app.MapGrpcService<AuthLookupService>();

        await app.StartAsync();
        return app;
    }
}
