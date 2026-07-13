using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.WebUI.Bff;

namespace Wms.WebUI.IntegrationTests.TestSupport;

// Menjalankan WebUI asli untuk test, dengan gateway diarahkan ke server test.
internal sealed class WebUiFactory(string gatewayAddress, bool enableEntra = false) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Konfigurasi service discovery mengarahkan nama "gateway" ke server test, mirip cara Aspire menginjek endpoint saat aplikasi jalan normal.
        var authority = new Uri(gatewayAddress).Authority;
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Bff:GatewayAddress"] = "http://gateway",
                ["Services:gateway:http:0"] = authority,
            }));

        // Aktifkan login Entra pada pengujian dengan mengganti konfigurasi BFF menggunakan nilai khusus test.
        if (enableEntra)
        {
            builder.ConfigureServices(services => services.AddSingleton(new EntraBffOptions
            {
                TenantId = "test-tenant",
                ClientId = "test-client",
                ClientSecret = "test-secret",
            }));
        }
    }
}
