using System.Collections.Concurrent;

namespace Wms.WebUI.Bff;

// Menyimpan token di sisi server. Browser cukup memegang cookie sesi HttpOnly, bukan JWT.
public interface ITokenStore
{
    void Set(string sessionId, TokenSet tokens);

    TokenSet? Get(string sessionId);

    void Remove(string sessionId);
}

// Implementasi lokal memakai memory store; deployment multi-instance perlu store terdistribusi seperti Redis.
internal sealed class InMemoryTokenStore : ITokenStore
{
    private readonly ConcurrentDictionary<string, TokenSet> _tokens = new(StringComparer.Ordinal);

    public void Set(string sessionId, TokenSet tokens) => _tokens[sessionId] = tokens;

    public TokenSet? Get(string sessionId) => _tokens.TryGetValue(sessionId, out var tokens) ? tokens : null;

    public void Remove(string sessionId) => _tokens.TryRemove(sessionId, out _);
}
