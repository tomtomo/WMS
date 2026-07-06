using Wms.Auth.Domain;

namespace Wms.Auth.Application.Abstractions;

// Write side User (tracked). Commit oleh TransactionBehavior via IUnitOfWork.
public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task<User?> GetAsync(UserId id, CancellationToken cancellationToken = default);

    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
}
