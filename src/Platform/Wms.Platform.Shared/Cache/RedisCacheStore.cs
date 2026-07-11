using System.Text.Json;
using StackExchange.Redis;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Shared.Cache;

// Adapter cache Redis dipakai bersama oleh Azure Managed Redis dan GCP Memorystore.
// Masa berlaku data ditangani langsung oleh Redis, jadi aplikasi tidak perlu membersihkannya sendiri.
public sealed class RedisCacheStore(IConnectionMultiplexer multiplexer) : ICacheStore
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    private IDatabase Database => multiplexer.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var value = await Database.StringGetAsync(key).ConfigureAwait(false);
        if (value.IsNullOrEmpty)
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(value.ToString(), _serializerOptions);
        }
        catch (JsonException)
        {
            // Jika format data cache tidak sesuai, anggap sebagai cache miss agar data diambil ulang dari sumbernya.
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan timeToLive, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        // Jangan simpan hasil kosong agar data yang baru dibuat tetap bisa ditemukan pada lookup berikutnya.
        if (value is null || timeToLive <= TimeSpan.Zero)
        {
            return;
        }

        await Database
            .StringSetAsync(key, JsonSerializer.Serialize(value, _serializerOptions), timeToLive)
            .ConfigureAwait(false);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await Database.KeyDeleteAsync(key).ConfigureAwait(false);
    }
}
