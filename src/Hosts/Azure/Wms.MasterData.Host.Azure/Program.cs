using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Wms.Auth.Grpc.Client;
using Wms.Auth.Grpc.V1;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Web;
using Wms.MasterData.Api.Endpoints;
using Wms.MasterData.Api.GrpcServices;
using Wms.MasterData.Application;

var builder = WebApplication.CreateBuilder(args);

// Kestrel memakai HTTP/1 dan HTTP/2 agar REST dan gRPC bisa menggunakan endpoint yang sama.
// Di dalam container, portnya dipisah lewat appsettings: 8080 untuk HTTP/1 dan 8081 untuk HTTP/2 (h2c).
builder.WebHost.ConfigureKestrel(kestrel =>
    kestrel.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http1AndHttp2));

builder.AddServiceDefaults();

// Daftarkan application pipeline, modul Masterdata, infrastructure, dan adapter Azure.
builder.Services.AddApplicationBuildingBlocks(typeof(MasterDataPermissions).Assembly);
builder.Services.AddBuildingBlocksInfrastructure("wms-masterdata");
builder.Services.AddMasterDataModule(builder.Configuration);
builder.Services.AddAzurePlatform(builder.Configuration);

// Endpoint web MasterData: REST, gRPC lookup, autentikasi JWT, user dari HttpContext, permission policy, dan fallback deny by default.
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

// Dibuka untuk WebApplicationFactory dan testing.
public partial class Program
{
    protected Program()
    {
    }
}
