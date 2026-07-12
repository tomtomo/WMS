using Microsoft.AspNetCore.Authorization;
using Wms.Auth.Grpc.Client;
using Wms.Auth.Grpc.V1;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Web;
using Wms.Notifications.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// REST: hanya melayani inbox API. proses pembagian dan pengiriman notifikasi dijalankan oleh Functions.
// Tanpa notifier, DeliveryDispatcherWorker akan berhenti sendiri karena IInAppNotifier tidak tersedia.
builder.Services.AddApplicationBuildingBlocks(typeof(NotificationsDbContext).Assembly);
builder.Services.AddBuildingBlocksInfrastructure("wms-notifications-read");
builder.Services.AddNotificationsModule(builder.Configuration);
builder.Services.AddAzurePlatform(builder.Configuration);

// Checker user aktif lintas host ke Auth
var authLookupAddress = new Uri(
    builder.Configuration["Services:Auth:Grpc"]
    ?? throw new InvalidOperationException("Konfigurasi 'Services:Auth:Grpc' wajib ada (di-inject AppHost/IaC)."));
builder.Services.AddInternalGrpcClient<AuthLookup.AuthLookupClient>(authLookupAddress);
builder.Services.AddSingleton<IActiveUserChecker, AuthGrpcActiveUserChecker>();

// Endpoint web Notifications: REST inbox, autentikasi JWT, user dari HttpContext, permission policy, dan fallback deny by default.
builder.Services.AddWebBuildingBlocks();
builder.Services.AddJwtBearerRs256(builder.Configuration);
builder.Services.AddHttpContextCurrentUser();
builder.Services.AddPermissionAuthorization();
builder.Services.Configure<AuthorizationOptions>(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

app.UseWebBuildingBlocks();
app.UseAuthentication();
app.UseIsActiveUserCheck();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapEndpoints(typeof(NotificationsDbContext).Assembly);

await app.RunAsync();

// Dibuka untuk WebApplicationFactory dan testing.
public partial class Program
{
    protected Program()
    {
    }
}
