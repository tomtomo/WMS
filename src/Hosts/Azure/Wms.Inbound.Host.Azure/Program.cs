using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Wms.Auth.Grpc.Client;
using Wms.Auth.Grpc.V1;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Web;
using Wms.Inbound.Api.Endpoints;
using Wms.Inbound.Api.GrpcServices;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Application.Features.CreateGoodsReceiptHeader;
using Wms.Inbound.Infrastructure.Grpc;
using Wms.MasterData.Grpc.V1;

var builder = WebApplication.CreateBuilder(args);

// Kestrel memakai HTTP/1 dan HTTP/2 agar REST dan gRPC bisa menggunakan endpoint yang sama.
// Di dalam container, portnya dipisah lewat appsettings: 8080 untuk HTTP/1 dan 8081 untuk HTTP/2 (h2c).
builder.WebHost.ConfigureKestrel(kestrel =>
    kestrel.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http1AndHttp2));

builder.AddServiceDefaults();

// Daftarkan application pipeline, modul Inbound, infrastructure, dan adapter Azure.
builder.Services.AddApplicationBuildingBlocks(typeof(CreateGoodsReceiptHeaderCommand).Assembly);
builder.Services.AddBuildingBlocksInfrastructure("wms-inbound");
builder.Services.AddInboundModule(builder.Configuration);
builder.Services.AddAzurePlatform(builder.Configuration);

// Reader MasterData memakai gRPC antar service, dengan endpoint yang diinjek dari IaC.
var masterDataAddress = new Uri(
    builder.Configuration["Services:MasterData:Grpc"]
    ?? throw new InvalidOperationException("Konfigurasi 'Services:MasterData:Grpc' wajib ada (di-inject IaC)."));
builder.Services.AddInternalGrpcClient<MasterDataLookup.MasterDataLookupClient>(masterDataAddress);
builder.Services.AddScoped<IProductReader, ProductGrpcReader>();
builder.Services.AddScoped<IWarehouseReader, WarehouseGrpcReader>();

// Endpoint web Inbound: REST, gRPC, autentikasi JWT, user dari HttpContext, permission policy, dan fallback deny by default.
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

// Inbound publish event lewat outbox, tapi tidak consume event.
builder.Services.AddEventingRail("wms.inbound");

var app = builder.Build();

app.UseWebBuildingBlocks();
app.UseAuthentication();
app.UseIsActiveUserCheck();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapEndpoints(typeof(CreateGoodsReceiptEndpoint).Assembly);

// Endpoint gRPC internal tidak menggunakan JWT dan hanya boleh diakses dari jaringan internal.
app.MapGrpcService<GoodsReceiptGrpcService>().AllowAnonymous();

// Tidak ada endpoint /files di sini: object store Azure memakai Blob Storage dengan SAS user delegation,
// jadi browser membaca langsung dari Storage, bukan lewat host.
await app.RunAsync();

// Dibuka untuk WebApplicationFactory dan testing.
public partial class Program
{
    protected Program()
    {
    }
}
