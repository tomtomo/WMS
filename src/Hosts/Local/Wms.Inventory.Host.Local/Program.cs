using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Wms.BuildingBlocks.Web;
using Wms.Inventory.Api.Endpoints;
using Wms.Inventory.Api.GrpcServices;
using Wms.Inventory.Application.Features.DetectNearExpiry;

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
builder.Services.AddHangfireServer();

// Endpoint web Inventory: REST, gRPC, autentikasi JWT, user dari HttpContext, permission policy, dan fallback deny-by-default.
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
app.MapGrpcService<InventoryReadGrpcService>();

await app.RunAsync();

// Dibuka untuk WebApplicationFactory dan testing Aspire.
public partial class Program
{
    protected Program()
    {
    }
}
