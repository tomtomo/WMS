using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;
using Wms.WebUI.Bff;
using Wms.WebUI.Components;
using Wms.WebUI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Blazor Web App dengan InteractiveServer dan MudBlazor. UI membaca data lewat BFF.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();

// Gunakan header dari proxy App Service agar redirect OIDC tetap memakai skema HTTPS.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// BFF memakai cookie HttpOnly, token server side, dan HTTP client gateway yang otomatis membawa bearer token serta correlation id.
builder.Services.AddWebUiBff(builder.Configuration);

// Daftarkan typed client untuk API Master Data beserta resolver ID ke nama melalui gateway BFF.
builder.Services.AddWmsApiClients();

var app = builder.Build();

app.UseForwardedHeaders();

// Sajikan aset RCL agar JavaScript dan CSS MudBlazor tersedia saat circuit interaktif dirender.
app.UseStaticFiles();

app.UseCorrelationId();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapDefaultEndpoints();
app.MapBffEndpoints();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.RunAsync();

// Dibuka untuk WebApplicationFactory.
public partial class Program
{
    protected Program()
    {
    }
}
