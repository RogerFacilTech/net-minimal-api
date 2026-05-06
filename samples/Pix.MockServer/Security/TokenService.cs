using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Pix.MockServer.Security;

public interface ITokenService
{
    string IssueToken(string clientId, string scope, TimeSpan ttl);
    bool ValidateToken(string token);
}

public sealed class TokenService : ITokenService
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _tokens = new();

    public string IssueToken(string clientId, string scope, TimeSpan ttl)
    {
        var random = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var token = $"pixmock.{clientId}.{scope}.{random}";
        _tokens[token] = DateTimeOffset.UtcNow.Add(ttl);
        return token;
    }

    public bool ValidateToken(string token)
    {
        if (!_tokens.TryGetValue(token, out var expiresAt))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow > expiresAt)
        {
            _tokens.TryRemove(token, out _);
            return false;
        }

        return true;
    }
}
