using System.Collections.Concurrent;

namespace Wms.WebUI.Bff;

// Simpan profil dan foto pengguna di server selama sesi untuk ditampilkan di navbar.
public interface IUserProfileStore
{
    void Set(string sessionId, UserProfile profile);

    UserProfile? Get(string sessionId);

    void Remove(string sessionId);
}

public sealed record UserProfile(string? DisplayName, string? PhotoDataUri);

internal sealed class InMemoryUserProfileStore : IUserProfileStore
{
    private readonly ConcurrentDictionary<string, UserProfile> _profiles = new(StringComparer.Ordinal);

    public void Set(string sessionId, UserProfile profile) => _profiles[sessionId] = profile;

    public UserProfile? Get(string sessionId) => _profiles.TryGetValue(sessionId, out var profile) ? profile : null;

    public void Remove(string sessionId) => _profiles.TryRemove(sessionId, out _);
}
