using Wms.Auth.Domain;

namespace Wms.Auth.Application.Abstractions;

// Kelola penautan akun eksternal ke pengguna, sedangkan penyimpanan perubahan ditangani oleh unit of work.
public interface IUserExternalLoginRepository
{
    Task<UserId?> FindUserIdAsync(string provider, string subject, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string provider, string subject, CancellationToken cancellationToken = default);

    Task AddAsync(UserExternalLogin login, CancellationToken cancellationToken = default);
}
