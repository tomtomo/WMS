using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Application.Abstractions;

// Validasi token Entra dan ambil identitas pengguna tanpa mengubah alur login lokal maupun token internal.
public interface IEntraTokenValidator
{
    Task<Result<EntraIdentity>> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}

// Identitas pengguna dari Entra yang sudah tervalidasi, dengan ObjectId sebagai kunci penautan akun.
public sealed record EntraIdentity(string ObjectId, string? UserPrincipalName, string? DisplayName);
