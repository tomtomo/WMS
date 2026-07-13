namespace Wms.Auth.Infrastructure.Security;

// Konfigurasi login Microsoft Entra untuk satu tenant. Jika dinonaktifkan, aplikasi tetap menggunakan login lokal.
public sealed class EntraAuthOptions
{
    public const string SectionName = "Entra";

    public bool Enabled { get; set; }

    public string TenantId { get; set; } = string.Empty;

    // Client ID aplikasi WebUI yang menjadi tujuan token Entra.
    public string ClientId { get; set; } = string.Empty;

    // Alamat metadata OIDC khusus untuk test. Jika kosong, alamat dibuat dari TenantId.
    public string? MetadataAddress { get; set; }

    public string ResolveMetadataAddress() =>
        string.IsNullOrWhiteSpace(MetadataAddress)
            ? $"https://login.microsoftonline.com/{TenantId}/v2.0/.well-known/openid-configuration"
            : MetadataAddress;
}
