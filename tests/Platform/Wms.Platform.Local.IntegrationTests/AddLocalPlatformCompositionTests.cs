using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Platform.Local.IntegrationTests.TestSupport;
using Wms.Platform.Local.ObjectStore;
using Xunit;

namespace Wms.Platform.Local.IntegrationTests;

// Setiap port di port-spine harus resolve dari AddLocalPlatform. Port baru tanpa adapter Local akan diketahui.
[Collection(PostgresCollection.Name)]
public sealed class AddLocalPlatformCompositionTests(PostgresFixture fixture)
{
    private static readonly Type[] _portSpine =
    [
        typeof(IMessagePublisher),
        typeof(IMessageSubscriber),
        typeof(IIntegrationEventOutbox),
        typeof(IInboxGuard),
        typeof(IDeadLetterStore),
        typeof(IApiIdempotencyStore),
        typeof(IProjectionStore),
        typeof(ICacheStore),
        typeof(IObjectStore),
        typeof(ISecretProvider),
        typeof(IServiceTokenProvider),
        typeof(IPasswordHasher),
        typeof(IRecurringJobScheduler),
        typeof(IDelayedTaskQueue),
        typeof(IEmailSender),
        typeof(IPushNotifier),
        typeof(IInAppNotifier),
        typeof(IAuditLogStore),
        typeof(ITelemetrySink),
        typeof(ISagaOrchestrator),
        typeof(IEventStreamPublisher),
        typeof(IEventStreamConsumer),
        typeof(IAnalyticsSink),
    ];

    [Fact]
    public async Task Every_port_in_the_spine_resolves_from_add_local_platform()
    {
        var connectionString = await fixture.CreateFreshDatabaseAsync();
        var configuration = BuildConfiguration(connectionString);
        var services = new ServiceCollection();
        services.AddLocalPlatform(configuration);

        // Pengganti DbContext modul
        services.AddDbContext<CompositionRailDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<CompositionRailDbContext>());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        foreach (var port in _portSpine)
        {
            scope.ServiceProvider.GetRequiredService(port).Should().NotBeNull(because: $"port {port.Name} wajib punya adapter Local");
        }
    }

    [Fact]
    public async Task Object_store_interface_and_concrete_share_one_instance_for_hmac_key_consistency()
    {
        var connectionString = await fixture.CreateFreshDatabaseAsync();
        var services = new ServiceCollection();
        services.AddLocalPlatform(BuildConfiguration(connectionString));
        using var provider = services.BuildServiceProvider();

        var byInterface = provider.GetRequiredService<IObjectStore>();
        var concrete = provider.GetRequiredService<FileSystemObjectStore>();

        byInterface.Should().BeSameAs(concrete);
    }

    private static IConfiguration BuildConfiguration(string connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new KeyValuePair<string, string?>[]
            {
                new("ConnectionStrings:wms", connectionString),
                new("ConnectionStrings:rabbitmq", "amqp://guest:guest@localhost:5672"),
                new("LocalPlatform:ObjectStore:RootPath", Path.Combine(Path.GetTempPath(), "wms-composition-objstore")),
                new("LocalPlatform:ObjectStore:BaseUrl", "http://localhost:5099/objects"),
            })
            .Build();
}
