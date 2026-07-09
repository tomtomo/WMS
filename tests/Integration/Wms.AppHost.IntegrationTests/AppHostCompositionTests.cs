using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Wms.AppHost.IntegrationTests;

// Memastikan AppHost merakit stack lokal lengkap: host modul, gateway, WebUI, migration runner, Postgres per modul, dan RabbitMQ.
public sealed class AppHostCompositionTests
{
    [Fact]
    public async Task AppHost_composes_full_local_stack()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Wms_AppHost>();
        await using var app = await appHost.BuildAsync();

        var resourceNames = app.Services.GetRequiredService<DistributedApplicationModel>()
            .Resources.Select(resource => resource.Name).ToHashSet(StringComparer.Ordinal);

        resourceNames.Should().Contain(
        [
            "postgres", "rabbitmq", "migrations",
            "wms-inbound", "wms-inventory", "wms-outbound", "wms-masterdata", "wms-auth",
            "wms-reporting", "wms-notifications", "wms-gateway", "wms-webui",
        ]);
    }
}
