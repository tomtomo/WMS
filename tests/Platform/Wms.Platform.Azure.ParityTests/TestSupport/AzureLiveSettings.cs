namespace Wms.Platform.Azure.ParityTests.TestSupport;

// Beberapa layanan Azure tidak memiliki emulator yang setara, jadi test memakai resource nyata.
// Jika konfigurasi environment belum tersedia, test akan diskip dengan alasan yang jelas.
internal static class AzureLiveSettings
{
    public static string? CosmosConnectionString => Read("WMS_PARITY_COSMOS_CONN");

    public static string? BlobAccountUrl => Read("WMS_PARITY_BLOB_URL");

    public static string? BlobContainerName => Read("WMS_PARITY_BLOB_CONTAINER");

    public static string? KeyVaultUri => Read("WMS_PARITY_KEYVAULT_URI");

    public static bool HasCosmos => !string.IsNullOrWhiteSpace(CosmosConnectionString);

    public static bool HasBlob => !string.IsNullOrWhiteSpace(BlobAccountUrl) && !string.IsNullOrWhiteSpace(BlobContainerName);

    public static bool HasKeyVault => !string.IsNullOrWhiteSpace(KeyVaultUri);

    private static string? Read(string name) => Environment.GetEnvironmentVariable(name);
}
