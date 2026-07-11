using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Azure.Persistence;

// Hapus cache saat projection berubah agar pembacaan berikutnya mengambil data terbaru.
// Handler lain, seperti update projection tambahan, dapat ditambahkan melalui alur yang sama.
public sealed class CacheInvalidatingProjectionChangeHandler(
    ICacheStore cacheStore,
    ILogger<CacheInvalidatingProjectionChangeHandler> logger) : IProjectionChangeHandler
{
    public static string CacheKeyFor(ProjectionChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        return $"projection:{change.ProjectionType}:{change.Key}";
    }

    public async Task HandleAsync(
        IReadOnlyCollection<ProjectionChange> changes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(changes);

        foreach (var change in changes)
        {
            await cacheStore.RemoveAsync(CacheKeyFor(change), cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Change feed projection menginvalidasi {ChangeCount} entri cache", changes.Count);
    }
}
