using System.Text.Json.Serialization;

namespace Pix.MockServer.Contracts;

public sealed record OAuthTokenRequest(
    [property: JsonPropertyName("client_id")]
    string ClientId,
    [property: JsonPropertyName("client_secret")]
    string ClientSecret,
    [property: JsonPropertyName("grant_type")]
    string GrantType);

public sealed record OAuthTokenResponse(
    [property: JsonPropertyName("access_token")]
    string AccessToken,
    [property: JsonPropertyName("token_type")]
    string TokenType,
    [property: JsonPropertyName("expires_in")]
    int ExpiresIn,
    string Scope);
