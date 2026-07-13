using System.Security.Claims;

namespace Wms.WebUI.Bff;

// Cek permission pada cookie untuk menentukan tampilan UI. Otorisasi tetap diperiksa di server.
internal static class ClaimsPrincipalExtensions
{
    public static bool HasPermission(this ClaimsPrincipal user, string permission) =>
        user.Claims.Any(claim =>
            claim.Type == BffClaims.Permission && string.Equals(claim.Value, permission, StringComparison.Ordinal));
}

// Salinan permission modul yang digunakan untuk mengatur tampilan UI.
internal static class UiPermissions
{
    public const string ReadGR = "Inbound.ReadGR";

    public const string CreateGR = "Inbound.CreateGR";

    public const string ScanGR = "Inbound.ScanGR";

    public const string CompleteScanGR = "Inbound.CompleteScanGR";

    public const string PostGR = "Inbound.PostGR";

    public const string CompletePutaway = "Inventory.CompletePutaway";

    public const string ReadStock = "Inventory.ReadStock";
}
