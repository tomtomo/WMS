namespace Wms.WebUI.Bff;

// Konfigurasi login Microsoft Entra di WebUI. Jika tenant atau client ID belum tersedia, login lokal tetap digunakan.
public sealed class EntraBffOptions
{
    public const string SectionName = "Entra";

    public string TenantId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    // Ambil client secret dari environment atau Key Vault, bukan dari appsettings maupun repository.
    public string ClientSecret { get; set; } = string.Empty;

    public string CallbackPath { get; set; } = "/signin-oidc";

    public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";

    // Alamat Microsoft Graph dapat disesuaikan untuk cloud Azure yang berbeda.
    public string GraphBaseAddress { get; set; } = string.Empty;

    public bool Enabled => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(TenantId);

    public string Authority => $"https://login.microsoftonline.com/{TenantId}/v2.0";
}
