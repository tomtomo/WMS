using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Wms.BuildingBlocks.Web;
using Wms.MasterData.Api.Endpoints;
using Wms.MasterData.Api.GrpcServices;
using Wms.MasterData.Application;

var builder = WebApplication.CreateBuilder(args);

// Kestrel memakai HTTP/1 dan HTTP/2 agar REST dan gRPC bisa berbagi endpoint yang sama.
builder.WebHost.ConfigureKestrel(kestrel =>
    kestrel.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http1AndHttp2));

builder.AddServiceDefaults();

// Pasang pipeline application, infrastructure, modul MasterData, dan adapter lokal.
builder.Services.AddApplicationBuildingBlocks(typeof(MasterDataPermissions).Assembly);
builder.Services.AddBuildingBlocksInfrastructure("wms-masterdata");
builder.Services.AddMasterDataModule(builder.Configuration);
builder.Services.AddLocalPlatform(builder.Configuration);

// Endpoint web MasterData: REST, gRPC lookup, autentikasi JWT, user dari HttpContext, permission policy, dan fallback deny by default.
builder.Services.AddWebBuildingBlocks();
builder.Services.AddGrpcWebBuildingBlocks();
builder.Services.AddJwtBearerRs256(builder.Configuration);
builder.Services.AddHttpContextCurrentUser();
builder.Services.AddPermissionAuthorization();
builder.Services.Configure<AuthorizationOptions>(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// MasterData publish event, tapi tidak consume event.
builder.Services.AddEventingRail("wms.masterdata");

var app = builder.Build();

app.UseWebBuildingBlocks();
app.UseAuthentication();
app.UseIsActiveUserCheck();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapEndpoints(typeof(WarehouseEndpoints).Assembly);

// Endpoint gRPC internal tidak menggunakan JWT dan hanya boleh diakses dari jaringan internal.
app.MapGrpcService<MasterDataLookupService>().AllowAnonymous();

await app.RunAsync();

// Dibuka untuk WebApplicationFactory dan testing Aspire.
public partial class Program
{
    protected Program()
    {
    }
}
