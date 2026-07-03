using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.ObjectStore;

// Object store filesystem (cloud: Blob Storage + SAS / GCS + Signed URL).
public sealed class FileSystemObjectStore : IObjectStore
{
    private readonly FileSystemObjectStoreOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly string _rootFullPath;
    private readonly byte[] _signingKey;

    public FileSystemObjectStore(IOptions<FileSystemObjectStoreOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _timeProvider = timeProvider;
        _rootFullPath = Path.GetFullPath(_options.RootPath);
        _signingKey = string.IsNullOrWhiteSpace(_options.SigningKeyBase64)
            ? RandomNumberGenerator.GetBytes(32)
            : Convert.FromBase64String(_options.SigningKeyBase64);
    }

    public async Task PutAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var fullPath = ResolveGuardedPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
        await using (stream.ConfigureAwait(false))
        {
            await content.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task<Stream> GetAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveGuardedPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Objek '{path}' tidak ditemukan.", fullPath);
        }

        return Task.FromResult<Stream>(
            new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true));
    }

    public Uri CreateReadUrl(string path, TimeSpan timeToLive)
    {
        ResolveGuardedPath(path);
        if (timeToLive <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeToLive), "TTL valet-key wajib positif.");
        }

        var encodedPath = EncodePathSegments(path);
        var expiresAtUnix = _timeProvider.GetUtcNow().Add(timeToLive).ToUnixTimeSeconds();
        var signature = ComputeSignature(encodedPath, expiresAtUnix);
        var baseUrl = _options.BaseUrl!.ToString().TrimEnd('/');

        return new Uri($"{baseUrl}/{encodedPath}?expires={expiresAtUnix.ToString(CultureInfo.InvariantCulture)}&sig={signature}");
    }

    // Verifikasi valet key oleh endpoint file Local: signature path scoped cocok dan belum kedaluwarsa.
    public bool TryValidateReadUrl(Uri readUrl, out string? path)
    {
        ArgumentNullException.ThrowIfNull(readUrl);
        path = null;

        var baseUrl = _options.BaseUrl!.ToString().TrimEnd('/');
        var absolute = readUrl.GetLeftPart(UriPartial.Path);
        if (!absolute.StartsWith(baseUrl + "/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var encodedCandidatePath = absolute[(baseUrl.Length + 1)..];
        var query = ParseQuery(readUrl.Query);
        if (!query.TryGetValue("expires", out var expiresRaw)
            || !query.TryGetValue("sig", out var signature)
            || !long.TryParse(expiresRaw, NumberStyles.None, CultureInfo.InvariantCulture, out var expiresAtUnix))
        {
            return false;
        }

        if (_timeProvider.GetUtcNow().ToUnixTimeSeconds() >= expiresAtUnix)
        {
            return false;
        }

        // Verifikasi atas bentuk encoded persis seperti yang ditandatangani CreateReadUrl.
        var expected = ComputeSignature(encodedCandidatePath, expiresAtUnix);
        var matches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
        if (!matches)
        {
            return false;
        }

        path = DecodePathSegments(encodedCandidatePath);
        return true;
    }

    private static string EncodePathSegments(string path) =>
        string.Join('/', path.Split('/').Select(Uri.EscapeDataString));

    private static string DecodePathSegments(string encodedPath) =>
        string.Join('/', encodedPath.Split('/').Select(Uri.UnescapeDataString));

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = pair.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex > 0)
            {
                result[pair[..separatorIndex]] = pair[(separatorIndex + 1)..];
            }
        }

        return result;
    }

    private string ComputeSignature(string path, long expiresAtUnix)
    {
        var payload = Encoding.UTF8.GetBytes($"{path}\n{expiresAtUnix.ToString(CultureInfo.InvariantCulture)}");
        var hash = HMACSHA256.HashData(_signingKey, payload);
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    // Hanya path relatif bersegmen '/' tanpa '..'/'.'; hasil akhir wajib tetap di bawah root.
    private string ResolveGuardedPath(string blobPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobPath);

        if (blobPath.Contains('\\', StringComparison.Ordinal)
            || blobPath.Contains(':', StringComparison.Ordinal)
            || Path.IsPathRooted(blobPath))
        {
            throw new ArgumentException($"blobPath '{blobPath}' ditolak: wajib relatif bersegmen '/'.", nameof(blobPath));
        }

        var segments = blobPath.Split('/');
        if (segments.Any(segment => segment.Length == 0 || segment is "." or ".."))
        {
            throw new ArgumentException($"blobPath '{blobPath}' ditolak: segmen kosong atau navigasi direktori.", nameof(blobPath));
        }

        var fullPath = Path.GetFullPath(Path.Combine(_rootFullPath, blobPath));
        if (!fullPath.StartsWith(_rootFullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"blobPath '{blobPath}' ditolak: keluar dari root object store.", nameof(blobPath));
        }

        return fullPath;
    }
}
