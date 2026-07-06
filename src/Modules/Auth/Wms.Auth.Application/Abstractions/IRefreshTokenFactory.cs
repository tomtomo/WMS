namespace Wms.Auth.Application.Abstractions;

// Membuat refresh token
public interface IRefreshTokenFactory
{
    RefreshTokenMaterial Create();

    string Hash(string rawToken);
}

// Raw dikirim ke klien sekali, TokenHash disimpan untuk lookup.
public sealed record RefreshTokenMaterial(string RawToken, string TokenHash);
