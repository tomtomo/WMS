using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Wms.BuildingBlocks.Web;
using Wms.MasterData.Grpc.V1;
using Wms.Outbound.Api.Endpoints;
using Wms.Outbound.Api.GrpcServices;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Application.Features.CreateWave;
using Wms.Outbound.Infrastructure.Grpc;

var builder = WebApplication.CreateBuilder(args);

// Kestrel memakai HTTP/1 dan HTTP/2 agar REST dan gRPC bisa menggunakan endpoint yang sama.
// Di dalam container, portnya dipisah lewat appsettings: 8080 untuk HTTP/1 dan 8081 untuk HTTP/2 (h2c).
builder.WebHost.ConfigureKestrel(kestrel =>
    kestrel.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http1AndHttp2));

builder.AddServiceDefaults();

// Daftarkan application pipeline, modul Outbound, infrastructure, dan adapter Azure.
builder.Services.AddApplicationBuildingBlocks(typeof(CreateWaveCommand).Assembly);
builder.Services.AddBuildingBlocksInfrastructure("wms-outbound");
builder.Services.AddOutboundModule(builder.Configuration);
builder.Services.AddAzurePlatform(builder.Configuration);

// Reader warehouse memakai gRPC ke MasterData, dengan endpoint yang diinjek dari IaC.
var masterDataAddress = new Uri(
    builder.Configuration["Services:MasterData:Grpc"]
    ?? throw new InvalidOperationException("Konfigurasi 'Services:MasterData:Grpc' wajib ada (di-inject IaC)."));
builder.Services.AddInternalGrpcClient<MasterDataLookup.MasterDataLookupClient>(masterDataAddress);
builder.Services.AddScoped<IWarehouseReader, WarehouseGrpcReader>();

// Endpoint web Outbound: REST, gRPC, autentikasi JWT, user dari HttpContext, permission policy, dan fallback deny by default.
builder.Services.AddWebBuildingBlocks();
builder.Services.AddGrpcWebBuildingBlocks();
builder.Services.AddJwtBearerRs256(builder.Configuration);
builder.Services.AddHttpContextCurrentUser();
builder.Services.AddPermissionAuthorization();
builder.Services.Configure<AuthorizationOptions>(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Outbound publish event dan juga consume event alokasi stok.
builder.Services.AddEventingRail("wms.outbound");
builder.Services.AddOutboundRailConsumers();

var app = builder.Build();

app.UseWebBuildingBlocks();
app.UseAuthentication();
app.UseIsActiveUserCheck();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapEndpoints(typeof(CreateWaveEndpoint).Assembly);

// Endpoint gRPC internal tidak menggunakan JWT dan hanya boleh diakses dari jaringan internal.
app.MapGrpcService<OutboundReadGrpcService>().AllowAnonymous();

await app.RunAsync();

// Dibuka untuk WebApplicationFactory dan testing.
public partial class Program
{
    protected Program()
    {
    }
}
