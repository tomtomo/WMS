namespace Wms.Reporting.Abstractions;

// Store untuk membaca dan memperbarui projection.
public interface IProjectionStore
{
    // Ambil projection berdasarkan key.
    Task<TProjection?> GetAsync<TProjection>(object[] key, CancellationToken cancellationToken = default)
        where TProjection : class;

    // Buat projection jika belum ada, lalu terapkan perubahan akumulatif.
    Task IncrementAsync<TProjection>(
        object[] key,
        Func<TProjection> create,
        Action<TProjection> increment,
        CancellationToken cancellationToken = default)
        where TProjection : class;

    // Kosongkan seluruh projection untuk rebuild.
    Task TruncateAllAsync(CancellationToken cancellationToken = default);
}
