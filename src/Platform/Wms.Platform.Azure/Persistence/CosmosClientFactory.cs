using System.Text.Json;
using Azure.Core;
using Microsoft.Azure.Cosmos;

namespace Wms.Platform.Azure.Persistence;

// Satu tempat pembuatan CosmosClient agar host dan test memakai serializer serta konsistensi yang sama.
public static class CosmosClientFactory
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    // Managed Identity tanpa access key(cloud).
    public static CosmosClient CreateWithManagedIdentity(Uri accountEndpoint, TokenCredential credential, CosmosOptions options)
    {
        ArgumentNullException.ThrowIfNull(accountEndpoint);
        return new CosmosClient(accountEndpoint.AbsoluteUri, credential, ClientOptions(options));
    }

    // Connection string hanya untuk emulator dan environment test.
    public static CosmosClient CreateWithConnectionString(string connectionString, CosmosOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        return new CosmosClient(connectionString, ClientOptions(options));
    }

    private static CosmosClientOptions ClientOptions(CosmosOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Gunakan System.Text.Json agar serializer Cosmos tetap sama dengan bagian lain di aplikasi.
        return new CosmosClientOptions
        {
            UseSystemTextJsonSerializerWithOptions = _serializerOptions,
            ConsistencyLevel = options.ConsistencyLevel,
        };
    }
}
