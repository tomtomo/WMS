using Wms.Auth.Domain;

namespace Wms.Auth.Application.Abstractions;

// Write side Role (tracked). Commit oleh TransactionBehavior via IUnitOfWork.
public interface IRoleRepository
{
    Task AddAsync(Role role, CancellationToken cancellationToken = default);

    Task<Role?> GetAsync(RoleId id, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
}
