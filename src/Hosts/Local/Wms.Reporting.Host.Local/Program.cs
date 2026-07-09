using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Wms.BuildingBlocks.Web;
using Wms.Reporting.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Kestrel tetap memakai HTTP/1 dan HTTP/2 agar pola host konsisten. Reporting sendiri hanya expose REST report, tanpa gRPC.
builder.WebHost.ConfigureKestrel(kestrel =>
    kestrel.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http1AndHttp2));

builder.AddServiceDefaults();

// Pasang pipeline application, infrastructure, modul Reporting, dan adapter lokal.
builder.Services.AddApplicationBuildingBlocks(typeof(ReportingDbContext).Assembly);
builder.Services.AddBuildingBlocksInfrastructure("wms-reporting");
builder.Services.AddReportingModule(builder.Configuration);
builder.Services.AddLocalPlatform(builder.Configuration);

// Endpoint web Reporting: REST report, autentikasi JWT, user dari HttpContext, permission policy, dan fallback deny-by-default.
builder.Services.AddWebBuildingBlocks();
builder.Services.AddJwtBearerRs256(builder.Configuration);
builder.Services.AddHttpContextCurrentUser();
builder.Services.AddPermissionAuthorization();
builder.Services.Configure<AuthorizationOptions>(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Reporting hanya consume event, jadi cukup daftarkan subscriber rail.
builder.Services.AddEventingRailSubscriber("wms.reporting");
builder.Services.AddReportingRailConsumers();

var app = builder.Build();

app.UseWebBuildingBlocks();
app.UseAuthentication();
app.UseIsActiveUserCheck();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapEndpoints(typeof(ReportingDbContext).Assembly);

await app.RunAsync();

// Dibuka untuk WebApplicationFactory dan testing Aspire.
public partial class Program
{
    protected Program()
    {
    }
}
