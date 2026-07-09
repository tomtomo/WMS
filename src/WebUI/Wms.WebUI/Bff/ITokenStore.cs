using System.Collections.Concurrent;

namespace Wms.WebUI.Bff;

// Menyimpan access token di sisi server. Browser cukup memegang cookie sesi HttpOnly, bukan JWT.
public interface ITokenStore
{
    void Set(string sessionId, string accessToken);

    string? Get(string sessionId);

    void Remove(string sessionId);
}

// Local single instance. Cloud (multi-instance)
internal sealed class InMemoryTokenStore : ITokenStore
{
    private readonly ConcurrentDictionary<string, string> _tokens = new(StringComparer.Ordinal);

    public void Set(string sessionId, string accessToken) => _tokens[sessionId] = accessToken;

    public string? Get(string sessionId) => _tokens.TryGetValue(sessionId, out var token) ? token : null;

    public void Remove(string sessionId) => _tokens.TryRemove(sessionId, out _);
}
