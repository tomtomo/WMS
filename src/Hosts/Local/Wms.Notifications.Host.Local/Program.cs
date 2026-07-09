using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Wms.Auth.Grpc.V1;
using Wms.BuildingBlocks.Web;
using Wms.Notifications.Abstractions;
using Wms.Notifications.Persistence;
using Wms.Notifications.UserDirectory;

var builder = WebApplication.CreateBuilder(args);

// Kestrel tetap memakai HTTP/1 dan HTTP/2 agar pola host konsisten. Notifications sendiri hanya expose REST inbox dan worker, tanpa gRPC.
builder.WebHost.ConfigureKestrel(kestrel =>
    kestrel.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http1AndHttp2));

builder.AddServiceDefaults();

// Pasang pipeline application, infrastructure, modul Notifications, dan adapter lokal.
builder.Services.AddApplicationBuildingBlocks(typeof(NotificationsDbContext).Assembly);
builder.Services.AddBuildingBlocksInfrastructure("wms-notifications");
builder.Services.AddNotificationsModule(builder.Configuration);
builder.Services.AddLocalPlatform(builder.Configuration);

// User directory memakai gRPC ke Auth untuk membaca user dan anggota role. Endpoint-nya diinjek dari AppHost.
var authAddress = new Uri(
    builder.Configuration["Services:Auth:Grpc"]
    ?? throw new InvalidOperationException("Konfigurasi 'Services:Auth:Grpc' wajib ada (di-inject AppHost)."));
builder.Services.AddInternalGrpcClient<AuthLookup.AuthLookupClient>(authAddress);
builder.Services.AddScoped<IUserDirectory, AuthGrpcUserDirectory>();

// Endpoint web Notifications: REST inbox, autentikasi JWT, user dari HttpContext, permission policy, dan fallback deny by default.
builder.Services.AddWebBuildingBlocks();
builder.Services.AddJwtBearerRs256(builder.Configuration);
builder.Services.AddHttpContextCurrentUser();
builder.Services.AddPermissionAuthorization();
builder.Services.Configure<AuthorizationOptions>(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Notifications hanya consume event. DeliveryDispatcherWorker didaftarkan dari AddNotificationsModule.
builder.Services.AddEventingRailSubscriber("wms.notifications");
builder.Services.AddNotificationsRailConsumers();

var app = builder.Build();

app.UseWebBuildingBlocks();
app.UseAuthentication();
app.UseIsActiveUserCheck();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapEndpoints(typeof(NotificationsDbContext).Assembly);

await app.RunAsync();

// Dibuka untuk WebApplicationFactory dan testing Aspire.
public partial class Program
{
    protected Program()
    {
    }
}
