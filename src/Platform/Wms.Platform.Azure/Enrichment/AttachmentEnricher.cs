using System.Globalization;
using System.Security.Cryptography;

namespace Wms.Platform.Azure.Enrichment;

// Pisahkan proses checksum dan pengecekan enrichment dari Azure Function agar bisa ditest tanpa binding atau koneksi.
// Checksum disimpan sebagai metadata blob, dan file yang sudah memiliki sha256 tidak diproses ulang.
public static class AttachmentEnricher
{
    // Nama key metadata harus valid karena Azure Blob Storage menolak karakter yang tidak didukung.
    public const string ChecksumMetadataKey = "sha256";
    public const string EnrichedAtMetadataKey = "enrichedAt";

    // Anggap lampiran sudah diproses jika metadata sha256 sudah tersedia.
    public static bool IsAlreadyEnriched(IDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return metadata.ContainsKey(ChecksumMetadataKey);
    }

    public static string ComputeSha256Hex(ReadOnlySpan<byte> content) =>
        Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    // Ambil nama blob dari subject EG BlobCreated: /blobServices/default/containers/{c}/blobs/{name}.
    // Kembalikan null jika subject kosong, berasal dari container lain, atau tidak memiliki nama blob.
    public static string? TryExtractBlobName(string? subject, string containerName)
    {
        if (string.IsNullOrEmpty(subject))
        {
            return null;
        }

        var marker = $"/containers/{containerName}/blobs/";
        var index = subject.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
        {
            return null;
        }

        var blobName = subject[(index + marker.Length)..];
        return blobName.Length == 0 ? null : blobName;
    }

    // Metadata enrichment baru
    public static IDictionary<string, string>? BuildEnrichmentMetadata(
        ReadOnlySpan<byte> content,
        IDictionary<string, string> existingMetadata,
        DateTimeOffset enrichedAt)
    {
        if (IsAlreadyEnriched(existingMetadata))
        {
            return null;
        }

        return new Dictionary<string, string>(existingMetadata, StringComparer.Ordinal)
        {
            [ChecksumMetadataKey] = ComputeSha256Hex(content),
            [EnrichedAtMetadataKey] = enrichedAt.ToString("O", CultureInfo.InvariantCulture),
        };
    }
}
