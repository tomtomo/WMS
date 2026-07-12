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
using Wms.Platform.Local.ObjectStore;

var builder = WebApplication.CreateBuilder(args);

// Kestrel memakai HTTP/1 dan HTTP/2 agar REST dan gRPC bisa berbagi endpoint yang sama.
builder.WebHost.ConfigureKestrel(kestrel =>
    kestrel.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http1AndHttp2));

builder.AddServiceDefaults();

// Pasang pipeline application, infrastructure, modul Inbound, dan adapter lokal.
builder.Services.AddApplicationBuildingBlocks(typeof(CreateGoodsReceiptHeaderCommand).Assembly);
builder.Services.AddBuildingBlocksInfrastructure("wms-inbound");
builder.Services.AddInboundModule(builder.Configuration);
builder.Services.AddLocalPlatform(builder.Configuration);

// Reader MasterData memakai gRPC sungguhan antar service, dengan endpoint yang diinjek dari AppHost.
var masterDataAddress = new Uri(
    builder.Configuration["Services:MasterData:Grpc"]
    ?? throw new InvalidOperationException("Konfigurasi 'Services:MasterData:Grpc' wajib ada (di-inject AppHost)."));
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

// Serving file dari signed URL lokal. Untuk endpoint ini, signature HMAC di URL menjadi izin aksesnya.
app.MapGet("/files/{**path}", async (HttpContext httpContext, FileSystemObjectStore objectStore) =>
{
    var requestUrl = new Uri(
        $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.Path}{httpContext.Request.QueryString}");
    if (!objectStore.TryValidateReadUrl(requestUrl, out var objectPath) || objectPath is null)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    try
    {
        var stream = await objectStore.GetAsync(objectPath, httpContext.RequestAborted);
        return Results.Stream(stream, "application/octet-stream");
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound();
    }
}).AllowAnonymous();

await app.RunAsync();

// Dibuka untuk WebApplicationFactory dan testing Aspire.
public partial class Program
{
    protected Program()
    {
    }
}
