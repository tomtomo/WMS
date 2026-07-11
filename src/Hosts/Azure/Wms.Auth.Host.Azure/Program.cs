using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Wms.Auth.Api.Endpoints;
using Wms.Auth.Api.GrpcServices;
using Wms.Auth.Application;
using Wms.BuildingBlocks.Web;

var builder = WebApplication.CreateBuilder(args);

// Kestrel memakai HTTP/1 dan HTTP/2 agar REST dan gRPC bisa menggunakan endpoint yang sama.
// Di dalam container, portnya dipisah lewat appsettings: 8080 untuk HTTP/1 dan 8081 untuk HTTP/2 (h2c).
builder.WebHost.ConfigureKestrel(kestrel =>
    kestrel.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http1AndHttp2));

builder.AddServiceDefaults();

// Daftarkan application pipeline, modul Auth, infrastructure, dan adapter Azure.
builder.Services.AddApplicationBuildingBlocks(typeof(AuthPermissions).Assembly);
builder.Services.AddBuildingBlocksInfrastructure("wms-auth");
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddAzurePlatform(builder.Configuration);

// Endpoint web Auth: REST, gRPC lookup, autentikasi JWT, user dari HttpContext, permission policy, dan fallback deny by default.
builder.Services.AddWebBuildingBlocks();
builder.Services.AddGrpcWebBuildingBlocks();
builder.Services.AddJwtBearerRs256(builder.Configuration);
builder.Services.AddHttpContextCurrentUser();
builder.Services.AddPermissionAuthorization();
builder.Services.Configure<AuthorizationOptions>(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Auth belum publish atau consume event, tapi rail tetap didaftarkan agar pola host konsisten.
builder.Services.AddEventingRail("wms.auth");

var app = builder.Build();

app.UseWebBuildingBlocks();
app.UseAuthentication();
app.UseIsActiveUserCheck();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapEndpoints(typeof(AuthEndpoints).Assembly);

// Endpoint gRPC internal tidak menggunakan JWT dan hanya boleh diakses dari jaringan internal.
app.MapGrpcService<AuthLookupService>().AllowAnonymous();

await app.RunAsync();

// Dibuka untuk WebApplicationFactory dan testing.
public partial class Program
{
    protected Program()
    {
    }
}
