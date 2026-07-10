using AwesomeAssertions;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.Platform.Azure.Eventing;
using Wms.Platform.Azure.Messaging;
using Wms.Platform.Azure.Saga;
using Wms.Platform.Azure.Scheduling;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Composition root harus memasang adapter Azure di balik port yang sama.
public sealed class AzureCompositionTests
{
    [Fact]
    public async Task Add_azure_platform_binds_every_messaging_port_to_its_azure_adapter()
    {
        // Sender adapter memakai IAsyncDisposable, jadi provider harus ditutup lewat DisposeAsync.
        await using var provider = BuildProvider();

        provider.GetRequiredService<OutboxDispatcher>().Should().BeOfType<AzureOutboxDispatcher>();
        provider.GetRequiredService<IMessageSubscriber>().Should().BeOfType<ServiceBusMessageSubscriber>();
        provider.GetRequiredService<IEventStreamPublisher>().Should().BeOfType<EventHubsEventStreamPublisher>();
        provider.GetRequiredService<ISagaOrchestrator>().Should().BeOfType<DurableFunctionsSagaOrchestrator>();
        provider.GetRequiredService<IDelayedTaskQueue>().Should().BeOfType<ServiceBusScheduledDelayedTaskQueue>();
        provider.GetRequiredService<IRecurringJobScheduler>().Should().BeOfType<FunctionsTimerRecurringJobScheduler>();
        provider.GetRequiredService<ServiceBusDeadLetterStore>().Should().NotBeNull();
    }

    [Fact]
    public void Rail_invariant_services_stay_registered_for_the_module_db()
    {
        var services = NewServices();
        services.AddAzurePlatform(NewConfiguration());

        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IIntegrationEventOutbox));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IInboxGuard));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IDeadLetterStore));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IAuditLogStore));
    }

    [Fact]
    public async Task Missing_event_grid_configuration_fails_fast_on_options_resolve()
    {
        var services = NewServices();
        services.AddAzurePlatform(NewConfiguration(includeEventGrid: false));
        await using var provider = services.BuildServiceProvider();

        var resolve = () => provider.GetRequiredService<IOptions<AzureMessagingOptions>>().Value;

        resolve.Should().Throw<OptionsValidationException>("endpoint/key Event Grid wajib dikonfigurasi");
    }

    private static ServiceProvider BuildProvider()
    {
        var services = NewServices();
        services.AddAzurePlatform(NewConfiguration());
        return services.BuildServiceProvider();
    }

    private static ServiceCollection NewServices()
    {
        var services = new ServiceCollection();

        // DurableTaskClient milik host worker.
        services.AddSingleton(Substitute.For<DurableTaskClient>("composition-test"));
        return services;
    }

    private static IConfiguration NewConfiguration(bool includeEventGrid = true)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:servicebus"] =
                "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
            ["ConnectionStrings:eventhubs"] =
                "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
        };

        if (includeEventGrid)
        {
            values["AzurePlatform:Messaging:EventGridTopicEndpoint"] = "https://wms-notif.westeurope-1.eventgrid.azure.net/api/events";
            values["AzurePlatform:Messaging:EventGridTopicKey"] = "parity-test-key";
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
