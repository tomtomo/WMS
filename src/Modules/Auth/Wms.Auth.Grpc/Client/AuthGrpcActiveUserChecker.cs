using System.Collections.Concurrent;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Logging;
using Wms.Auth.Grpc.V1;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Auth.Grpc.Client;

// Cek status aktif user ke Auth melalui gRPC untuk membatasi penggunaan JWT setelah user dinonaktifkan.
// Jika Auth tidak tersedia, request tetap lanjut karena validasi JWT tetap menjadi pemeriksaan utama.
public sealed class AuthGrpcActiveUserChecker(
    GrpcClientFactory clientFactory,
    TimeProvider timeProvider,
    ILogger<AuthGrpcActiveUserChecker> logger) : IActiveUserChecker
{
    // Simpan hasil pengecekan selama 60 detik agar request berulang tidak terus memanggil Auth.
    private static readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(60);

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);

    public async Task<bool> IsActiveAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var now = timeProvider.GetUtcNow();
        if (_cache.TryGetValue(userId, out var entry) && entry.ExpiresAt > now)
        {
            return entry.IsActive;
        }

        try
        {
            var client = clientFactory.CreateClient<AuthLookup.AuthLookupClient>(nameof(AuthLookup.AuthLookupClient));
            var snapshot = await client.GetUserAsync(
                new GetUserRequest { UserId = userId }, cancellationToken: cancellationToken);

            _cache[userId] = new CacheEntry(snapshot.IsActive, now + _cacheTtl);
            return snapshot.IsActive;
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            // User yang tidak ditemukan di Auth dianggap tidak aktif.
            // Hasilnya disimpan sementara agar request dengan token yang sama tidak terus memanggil Auth.
            _cache[userId] = new CacheEntry(false, now + _cacheTtl);
            return false;
        }
        catch (RpcException exception)
        {
            logger.LogWarning(
                exception,
                "Cek user aktif '{UserId}' gagal ({StatusCode})",
                userId,
                exception.StatusCode);
            return true;
        }
    }

    private readonly record struct CacheEntry(bool IsActive, DateTimeOffset ExpiresAt);
}
