using MudBlazor.Services;
using Wms.WebUI.Bff;
using Wms.WebUI.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Blazor Web App dengan InteractiveServer dan MudBlazor. UI membaca data lewat BFF.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();

// BFF memakai cookie HttpOnly, token server side, dan HTTP client gateway yang otomatis membawa bearer token serta correlation id.
builder.Services.AddWebUiBff(builder.Configuration);

var app = builder.Build();

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
