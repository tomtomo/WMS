using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Wms.Platform.Azure.ObjectStore;
using Xunit;

namespace Wms.Platform.Azure.ParityTests.TestSupport;

// Azurite tidak mendukung GetUserDelegationKey, jadi Valet Key ditest memakai storage account.
// Autentikasi menggunakan DefaultAzureCredential tanpa account key.
public sealed class BlobFixture : IAsyncLifetime
{
    public BlobObjectStoreOptions StoreOptions { get; private set; } = new();

    public BlobServiceClient? ServiceClient { get; private set; }

    public bool IsAvailable => ServiceClient is not null;

    public async Task InitializeAsync()
    {
        if (!AzureLiveSettings.HasBlob)
        {
            return;
        }

        StoreOptions = new BlobObjectStoreOptions
        {
            AccountUrl = new Uri(AzureLiveSettings.BlobAccountUrl!),
            ContainerName = AzureLiveSettings.BlobContainerName!,
        };

        ServiceClient = new BlobServiceClient(StoreOptions.AccountUrl, new DefaultAzureCredential());
        await ServiceClient.GetBlobContainerClient(StoreOptions.ContainerName).CreateIfNotExistsAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public BlobObjectStore CreateStore() => new(ServiceClient!, Options.Create(StoreOptions), TimeProvider.System);
}

[CollectionDefinition(Name)]
public sealed class BlobCollection : ICollectionFixture<BlobFixture>
{
    public const string Name = "blob";
}
