using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Wms.Auth.Grpc.Client;
using Wms.Auth.Grpc.V1;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Web;
using Wms.Inventory.Api.Endpoints;
using Wms.Inventory.Api.GrpcServices;
using Wms.Inventory.Application.Features.DetectNearExpiry;
using Wms.Platform.Local.Streaming;

var builder = WebApplication.CreateBuilder(args);

// Kestrel memakai HTTP/1 dan HTTP/2 agar REST dan gRPC bisa berbagi endpoint yang sama.
builder.WebHost.ConfigureKestrel(kestrel =>
    kestrel.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http1AndHttp2));

builder.AddServiceDefaults();

// Pasang pipeline application, infrastructure, modul Inventory, dan adapter lokal.
builder.Services.AddApplicationBuildingBlocks(typeof(DetectNearExpiryCommand).Assembly);
builder.Services.AddBuildingBlocksInfrastructure("wms-inventory");
builder.Services.AddInventoryModule(builder.Configuration);
builder.Services.AddLocalPlatform(builder.Configuration);

// Jalankan Hangfire server untuk memproses recurring dan delayed job, termasuk expiry scan.
// Hangfire server hanya di Local.
builder.Services.AddHangfireServer();

// Endpoint web Inventory: REST, gRPC, autentikasi JWT, user dari HttpContext, permission policy, dan fallback deny-by-default.
// Checker user aktif lintas host ke Auth.
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

// Worker telemetry operasional
builder.Services.AddHostedService<OperationalTelemetryStreamWorker>();

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

// Dibuka untuk WebApplicationFactory dan testing Aspire.
public partial class Program
{
    protected Program()
    {
    }
}
