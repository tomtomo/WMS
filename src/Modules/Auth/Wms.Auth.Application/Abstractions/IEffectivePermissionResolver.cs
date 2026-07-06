using Wms.Auth.Domain;

namespace Wms.Auth.Application.Abstractions;

public interface IEffectivePermissionResolver
{
    Task<IReadOnlyCollection<string>> ResolveAsync(User user, CancellationToken cancellationToken = default);
}
