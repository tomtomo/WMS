using System.Globalization;

namespace Wms.WebUI.E2ETests;

// Ambil konfigurasi E2E dari environment agar test yang sama bisa dipakai di semua cloud.
// Nilai default mengikuti seed lokal dan bisa ditimpa untuk lingkungan non-dev.
public sealed class WmsE2EProfile
{
    public string? BaseUrl { get; private init; }

    public string AuthMode { get; private init; } = "local";

    public string Username { get; private init; } = "admin";

    public string Password { get; private init; } = "ChangeMe#2026";

    public string LowPermUsername { get; private init; } = "viewer";

    public string LowPermPassword { get; private init; } = "ChangeMe#2026";

    public bool IgnoreTls { get; private init; } = true;

    public string ReadinessPath { get; private init; } = "/health";

    public string EvidenceMode { get; private init; } = "db";

    public string? DbConnString { get; private init; }

    public string? GatewayUrl { get; private init; }

    // Channel kosong memakai Chromium bawaan CI; sandbox lokal tanpa VC++ runtime memakai Edge.
    public string? BrowserChannel { get; private init; }

    // Tanpa BaseUrl, SkippableFact melewati test agar aman dijalankan di PR.
    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl);

    public static WmsE2EProfile FromEnvironment() => new()
    {
        BaseUrl = Env("WMS_E2E_BASEURL")?.TrimEnd('/'),
        AuthMode = Env("WMS_E2E_AUTHMODE") ?? "local",
        Username = Env("WMS_E2E_USERNAME") ?? "admin",
        Password = Env("WMS_E2E_PASSWORD") ?? "ChangeMe#2026",
        LowPermUsername = Env("WMS_E2E_LOWPERM_USERNAME") ?? "viewer",
        LowPermPassword = Env("WMS_E2E_LOWPERM_PASSWORD") ?? "ChangeMe#2026",
        IgnoreTls = !string.Equals(Env("WMS_E2E_IGNORE_TLS"), "false", StringComparison.OrdinalIgnoreCase),
        ReadinessPath = Env("WMS_E2E_READINESS_PATH") ?? "/health",
        EvidenceMode = Env("WMS_E2E_EVIDENCE_MODE") ?? "db",
        DbConnString = Env("WMS_E2E_DB_CONNSTRING"),
        GatewayUrl = Env("WMS_E2E_GATEWAY_URL")?.TrimEnd('/'),
        BrowserChannel = Env("WMS_E2E_BROWSER_CHANNEL"),
    };

    // Tiap modul memakai database berbeda pada server yang sama, jadi hanya nama databasenya yang diganti.
    public string? DbFor(string database) =>
        DbConnString is null ? null : ReplaceDatabase(DbConnString, database);

    private static string ReplaceDatabase(string connString, string database)
    {
        var parts = connString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !part.StartsWith("Database=", StringComparison.OrdinalIgnoreCase)
                && !part.StartsWith("Db=", StringComparison.OrdinalIgnoreCase));
        return string.Join(';', parts.Append($"Database={database}"));
    }

    private static string? Env(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
