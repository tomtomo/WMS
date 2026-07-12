using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Wms.Auth.Grpc.Client;
using Wms.Auth.Grpc.V1;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Web;
using Wms.Inventory.Api.Endpoints;
using Wms.Inventory.Api.GrpcServices;
using Wms.Inventory.Application.Features.DetectNearExpiry;

var builder = WebApplication.CreateBuilder(args);

// Kestrel memakai HTTP/1 dan HTTP/2 agar REST dan gRPC bisa menggunakan endpoint yang sama.
// Di dalam container, portnya dipisah lewat appsettings: 8080 untuk HTTP/1 dan 8081 untuk HTTP/2 (h2c).
builder.WebHost.ConfigureKestrel(kestrel =>
    kestrel.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http1AndHttp2));

builder.AddServiceDefaults();

// Daftarkan application pipeline, modul Inventory, infrastructure, dan adapter Azure.
builder.Services.AddApplicationBuildingBlocks(typeof(DetectNearExpiryCommand).Assembly);
builder.Services.AddBuildingBlocksInfrastructure("wms-inventory");
builder.Services.AddInventoryModule(builder.Configuration);
builder.Services.AddAzurePlatform(builder.Configuration);

// Tidak ada Hangfire server di sini: scheduled job (expiry scan) dijalankan oleh timer trigger
// di Wms.Scheduled.Functions.Azure, dan IRecurringJobScheduler versi Azure hanya berperan sebagai katalog cron.

// Endpoint web Inventory: REST, gRPC, autentikasi JWT, user dari HttpContext, permission policy, dan fallback deny by default.
// Checker user aktif lintas host ke Auth
var authLookupAddress = new Uri(
    builder.Configuration["Services:Auth:Grpc"]
    ?? throw new InvalidOperationException("Konfigurasi 'Services:Auth:Grpc' wajib ada (diinject AppHost/IaC)."));
builder.Services.AddInternalGrpcClient<AuthLookup.AuthLookupClient>(authLookupAddress);
builder.Services.AddSingleton<IActiveUserChecker, AuthGrpcActiveUserChecker>();
builder.Services.AddWebBuildingBlocks();
builder.Services.AddGrpcWebBuildingBlocks();
builder.Services.AddJwtBearerRs256(builder.Configuration);
builder.Services.AddHttpContextCurrentUser();
builder.Services.AddPermissionAuthorization();
builder.Services.Configure<AuthorizationOptions>(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Inventory publish event dan juga consume event dari modul lain.
builder.Services.AddEventingRail("wms.inventory");
builder.Services.AddInventoryRailConsumers();

var app = builder.Build();

app.UseWebBuildingBlocks();
app.UseAuthentication();
app.UseIsActiveUserCheck();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapEndpoints(typeof(CompletePutawayEndpoint).Assembly);

// Endpoint gRPC internal tidak menggunakan JWT dan hanya boleh diakses dari jaringan internal.
app.MapGrpcService<InventoryReadGrpcService>().AllowAnonymous();

await app.RunAsync();

// Dibuka untuk WebApplicationFactory dan testing.
public partial class Program
{
    protected Program()
    {
    }
}
