using Microsoft.AspNetCore.Authorization;
using Wms.Auth.Grpc.Client;
using Wms.Auth.Grpc.V1;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Web;
using Wms.Reporting.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Thin REST host: hanya read API report
builder.Services.AddApplicationBuildingBlocks(typeof(ReportingDbContext).Assembly);
builder.Services.AddBuildingBlocksInfrastructure("wms-reporting-read");
builder.Services.AddReportingModule(builder.Configuration);
builder.Services.AddAzurePlatform(builder.Configuration);

// Checker userbaktif lintas host ke Auth
var authLookupAddress = new Uri(
    builder.Configuration["Services:Auth:Grpc"]
    ?? throw new InvalidOperationException("Konfigurasi 'Services:Auth:Grpc' wajib ada (diinject AppHost/IaC)."));
builder.Services.AddInternalGrpcClient<AuthLookup.AuthLookupClient>(authLookupAddress);
builder.Services.AddSingleton<IActiveUserChecker, AuthGrpcActiveUserChecker>();

// Endpoint web Reporting: REST report, autentikasi JWT, user dari HttpContext, permission policy, dan fallback deny-by-default.
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
app.MapEndpoints(typeof(ReportingDbContext).Assembly);

await app.RunAsync();

// Dibuka untuk WebApplicationFactory dan testing.
public partial class Program
{
    protected Program()
    {
    }
}
