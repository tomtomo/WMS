namespace Wms.WebUI.Services;

// Menyimpan mapping LocationId ke kode lokasi per circuit agar tabel menampilkan kode, bukan GUID, tanpa request berulang.
public sealed class LocationNameResolver(WmsApiClient api)
{
    // Batas ini cukup untuk data sandbox. Lokasi di luar batas akan ditampilkan sebagai ID ringkas.
    private const int MaxLocations = 500;

    private IReadOnlyList<LocationDto>? _locations;
    private IReadOnlyDictionary<Guid, string>? _codesById;

    // Load data lokasi sekali per circuit. Aman dipanggil berulang dari OnInitializedAsync.
    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_codesById is not null)
        {
            return;
        }

        // Sertakan lokasi nonaktif agar referensi pada data historis tetap bisa diterjemahkan.
        var result = await api.MasterData.ListLocationsAsync(1, MaxLocations, includeInactive: true, cancellationToken);
        _locations = result.Success ? result.Value!.Items : [];
        _codesById = _locations.ToDictionary(location => location.LocationId, location => location.Code);
    }

    // Cari kode lokasi langsung dari cache. Jika tidak ditemukan, tampilkan delapan karakter awal ID.
    public string Resolve(Guid locationId) =>
        _codesById is not null && _codesById.TryGetValue(locationId, out var code) ? code : locationId.ToString()[..8];

    // Ambil lokasi aktif dengan tipe tertentu, misalnya StagingArea, untuk pilihan operator.
    public IReadOnlyList<LocationDto> OfType(string type) =>
        _locations is null ? [] : [.. _locations.Where(location => location.IsActive && string.Equals(location.Type, type, StringComparison.Ordinal))];
}
