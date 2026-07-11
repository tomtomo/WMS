namespace Wms.Platform.Azure.Persistence;

// Menangani perubahan projection dari change feed, seperti invalidasi cache atau update projection lain.
// Jika proses ini gagal, penyimpanan projection utama tetap tidak ikut dibatalkan.
public interface IProjectionChangeHandler
{
    Task HandleAsync(IReadOnlyCollection<ProjectionChange> changes, CancellationToken cancellationToken = default);
}
