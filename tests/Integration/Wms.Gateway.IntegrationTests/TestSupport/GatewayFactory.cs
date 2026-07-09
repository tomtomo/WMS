using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Wms.Gateway.IntegrationTests.TestSupport;

// Menjalankan gateway asli untuk test, dengan public key JWT test dan satu route ke downstream test.
internal sealed class GatewayFactory(string publicKeyPem, string downstreamAddress) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:PublicKeyPem"] = publicKeyPem,
                ["ReverseProxy:Routes:test:ClusterId"] = "test",
                ["ReverseProxy:Routes:test:AuthorizationPolicy"] = "default",
                ["ReverseProxy:Routes:test:Match:Path"] = "/svc/{**catch-all}",
                ["ReverseProxy:Routes:test:Transforms:0:PathRemovePrefix"] = "/svc",
                ["ReverseProxy:Clusters:test:Destinations:host:Address"] = downstreamAddress,
            }));
    }
}
