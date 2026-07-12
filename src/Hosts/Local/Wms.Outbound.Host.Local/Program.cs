using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Wms.Auth.Grpc.Client;
using Wms.Auth.Grpc.V1;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Web;
using Wms.MasterData.Grpc.V1;
using Wms.Outbound.Api.Endpoints;
using Wms.Outbound.Api.GrpcServices;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Application.Features.CreateWave;
using Wms.Outbound.Infrastructure.Grpc;

var builder = WebApplication.CreateBuilder(args);

// Kestrel memakai HTTP/1 dan HTTP/2 agar REST dan gRPC bisa berbagi endpoint yang sama.
builder.WebHost.ConfigureKestrel(kestrel =>
    kestrel.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http1AndHttp2));

builder.AddServiceDefaults();

// Pasang pipeline application, infrastructure, modul Outbound, dan adapter lokal.
builder.Services.AddApplicationBuildingBlocks(typeof(CreateWaveCommand).Assembly);
builder.Services.AddBuildingBlocksInfrastructure("wms-outbound");
builder.Services.AddOutboundModule(builder.Configuration);
builder.Services.AddLocalPlatform(builder.Configuration);

// Reader warehouse memakai gRPC ke MasterData, dengan endpoint yang diinjek dari AppHost.
var masterDataAddress = new Uri(
    builder.Configuration["Services:MasterData:Grpc"]
    ?? throw new InvalidOperationException("Konfigurasi 'Services:MasterData:Grpc' wajib ada (diinject AppHost)."));
builder.Services.AddInternalGrpcClient<MasterDataLookup.MasterDataLookupClient>(masterDataAddress);
builder.Services.AddScoped<IWarehouseReader, WarehouseGrpcReader>();

// Endpoint web Outbound: REST, gRPC, autentikasi JWT, user dari HttpContext, permission policy, dan fallback deny-by-default.
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

// Dibuka untuk WebApplicationFactory dan testing Aspire.
public partial class Program
{
    protected Program()
    {
    }
}
