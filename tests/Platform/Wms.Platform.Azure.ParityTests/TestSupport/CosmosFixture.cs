using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Wms.Platform.Azure.Persistence;
using Xunit;

namespace Wms.Platform.Azure.ParityTests.TestSupport;

// Cosmos live di resource group khusus test: emulator Linux belum menyediakan change feed,
// jadi jalur ini dibuktikan terhadap akun serverless.
public sealed class CosmosFixture : IAsyncLifetime
{
    public CosmosOptions Options { get; } = new()
    {
        DatabaseName = "wms",
        ProjectionContainerName = "projections",
        LeaseContainerName = "projections-leases",
        ChangeFeedProcessorName = "parity-downstream",
        ChangeFeedInstanceName = $"parity-{Guid.NewGuid():N}",
        ChangeFeedPollInterval = TimeSpan.FromMilliseconds(500),
    };

    public CosmosClient? Client { get; private set; }

    public bool IsAvailable => Client is not null;

    public Task InitializeAsync()
    {
        if (AzureLiveSettings.HasCosmos)
        {
            Client = CosmosClientFactory.CreateWithConnectionString(AzureLiveSettings.CosmosConnectionString!, Options);
        }

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Client?.Dispose();
        return Task.CompletedTask;
    }

    public CosmosProjectionStore CreateStore() =>
        new(Client!, Microsoft.Extensions.Options.Options.Create(Options), TimeProvider.System);

    public IOptions<CosmosOptions> AsOptions() => Microsoft.Extensions.Options.Options.Create(Options);
}

[CollectionDefinition(Name)]
public sealed class CosmosCollection : ICollectionFixture<CosmosFixture>
{
    public const string Name = "cosmos";
}
