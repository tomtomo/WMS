using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Kestrel disiapkan untuk HTTP/1 dan HTTP/2, supaya gateway lokal bisa melayani REST proxy dengan dev certificate.
builder.WebHost.ConfigureKestrel(kestrel =>
    kestrel.ConfigureEndpointDefaults(endpoint => endpoint.Protocols = HttpProtocols.Http1AndHttp2));

builder.AddServiceDefaults();

// Reverse proxy YARP memakai Aspire service discovery, jadi tujuan route bisa dicari dari nama resource.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

// Gateway memvalidasi JWT RS256 dan menolak request anonim secara default. Header Authorization tetap diteruskan YARP ke service downstream.
builder.Services.AddJwtBearerRs256(builder.Configuration);
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

// Pasang X-Correlation-ID sebelum request diteruskan ke downstream.
app.UseCorrelationId();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapReverseProxy();

await app.RunAsync();

// Dibuka untuk WebApplicationFactory dan testing Aspire.
public partial class Program
{
    protected Program()
    {
    }
}
