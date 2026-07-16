namespace Wms.WebUI.Bff;

// Menyimpan access token, refresh token, dan waktu kedaluwarsa access token untuk setiap sesi.
public sealed record TokenSet(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
