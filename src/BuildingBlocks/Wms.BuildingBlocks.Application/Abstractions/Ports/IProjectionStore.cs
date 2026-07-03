namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Store read-model projection untuk Reporting: Postgres Local, Cosmos Azure, Firestore GCP.
public interface IProjectionStore
{
    Task UpsertAsync<TProjection>(string key, TProjection projection, CancellationToken cancellationToken = default)
        where TProjection : class;

    Task<TProjection?> GetAsync<TProjection>(string key, CancellationToken cancellationToken = default)
        where TProjection : class;
}
