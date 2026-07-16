namespace Wms.WebUI.Services;

// Menyimpan mapping WarehouseId ke nama per circuit agar tabel menampilkan nama warehouse tanpa request berulang per baris.
public sealed class WarehouseNameResolver(WmsApiClient api)
{
    // Batas ini cukup untuk data sandbox. Warehouse di luar batas akan ditampilkan sebagai GUID.
    private const int MaxWarehouses = 200;

    private IReadOnlyDictionary<Guid, string>? _namesById;

    // Muat data warehouse sekali per circuit. Aman dipanggil berulang dari lifecycle halaman maupun MudTable ServerData.
    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_namesById is not null)
        {
            return;
        }

        // Sertakan warehouse nonaktif agar referensi pada data historis tetap bisa diterjemahkan.
        var result = await api.MasterData.ListWarehousesAsync(1, MaxWarehouses, includeInactive: true, cancellationToken);
        _namesById = result.Success
            ? result.Value!.Items.ToDictionary(warehouse => warehouse.WarehouseId, warehouse => warehouse.Name)
            : new Dictionary<Guid, string>(0);
    }

    // Ambil nama warehouse dari cache. Jika tidak ditemukan, tampilkan GUID aslinya.
    public string Resolve(Guid warehouseId) =>
        _namesById is not null && _namesById.TryGetValue(warehouseId, out var name) ? name : warehouseId.ToString();
}
