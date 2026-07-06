namespace Wms.Auth.Application;

// Default umur refresh token. Dapat dikonfigurasi sesuai kebutuhan.
internal static class AuthTokenDefaults
{
    internal static readonly TimeSpan _refreshTokenLifetime = TimeSpan.FromDays(7);
}
