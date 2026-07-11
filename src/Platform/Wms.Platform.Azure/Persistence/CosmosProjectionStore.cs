using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Azure.Persistence;

// Projection read side disimpan sebagai dokumen NoSQL dan dapat dibuat ulang dari event.
// Cosmos menggantikan jsonb PostgreSQL lokal tanpa mengubah interface yang digunakan.
public sealed class CosmosProjectionStore : IProjectionStore
{
    private readonly Container _container;
    private readonly TimeProvider _timeProvider;

    public CosmosProjectionStore(CosmosClient client, IOptions<CosmosOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        _container = client.GetContainer(options.Value.DatabaseName, options.Value.ProjectionContainerName);
        _timeProvider = timeProvider;
    }

    public async Task UpsertAsync<TProjection>(
        string key,
        TProjection projection,
        CancellationToken cancellationToken = default)
        where TProjection : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(projection);

        var document = new ProjectionDocument<TProjection>
        {
            Id = DocumentId<TProjection>(key),
            PartitionKey = key,
            ProjectionType = ProjectionTypeName<TProjection>(),
            Document = projection,
            UpdatedAt = _timeProvider.GetUtcNow(),
        };

        await _container
            .UpsertItemAsync(document, new PartitionKey(key), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TProjection?> GetAsync<TProjection>(string key, CancellationToken cancellationToken = default)
        where TProjection : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var response = await _container
                .ReadItemAsync<ProjectionDocument<TProjection>>(
                    DocumentId<TProjection>(key),
                    new PartitionKey(key),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return response.Resource.Document;
        }
        catch (CosmosException notFound) when (notFound.StatusCode == HttpStatusCode.NotFound)
        {
            // Projection yang belum pernah ditulis dianggap miss biasa, sama seperti pada implementasi Postgres.
            return null;
        }
    }

    internal static string ProjectionTypeName<TProjection>() => typeof(TProjection).FullName ?? typeof(TProjection).Name;

    // id Cosmos tidak boleh terdapat '/', '\', '?', atau '#', jadi key diescape agar gabungan tipe dan key tetap unik.
    private static string DocumentId<TProjection>(string key) =>
        $"{ProjectionTypeName<TProjection>()}:{Uri.EscapeDataString(key)}";
}
