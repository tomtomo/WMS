using System.Text.Json;
using Azure.Core;
using Microsoft.Azure.Cosmos;

namespace Wms.Platform.Azure.Persistence;

// Satu tempat pembuatan CosmosClient, agar host dan test memakai serializer serta konsistensi yang sama.
public static class CosmosClientFactory
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    // Di production, gunakan Managed Identity tanpa access key.
    public static CosmosClient CreateWithManagedIdentity(Uri accountEndpoint, TokenCredential credential, CosmosOptions options)
    {
        ArgumentNullException.ThrowIfNull(accountEndpoint);
        return new CosmosClient(accountEndpoint.AbsoluteUri, credential, ClientOptions(options));
    }

    // Connection string hanya digunakan untuk emulator dan environment test.
    public static CosmosClient CreateWithConnectionString(string connectionString, CosmosOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        return new CosmosClient(connectionString, ClientOptions(options));
    }

    private static CosmosClientOptions ClientOptions(CosmosOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new CosmosClientOptions
        {
            // SDK Cosmos memakai Newtonsoft secara default, sedangkan seluruh repo memakai System.Text.Json.
            // Memakai opsi bawaan SDK ini menghindari CosmosSerializer buatan sendiri yang rawan salah pada stream kosong.
            UseSystemTextJsonSerializerWithOptions = _serializerOptions,
            ConsistencyLevel = options.ConsistencyLevel,
        };
    }
}
