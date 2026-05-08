using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Catalogo.Tests.Integration;

public class AuthHelperTests
{
    [Fact]
    public async Task ObterTokenAsync_ComAuthBaseUrlERetornoTokenInvalido_DeveLancarErroClaro()
    {
        using var server = FakeAuthServer.Start("{\"token\":\"nao-e-jwt\"}");
        using var scope = new EnvironmentScope(
            ("AUTH_BASE_URL", server.BaseUrl),
            ("JWT_ISSUER", "FacShopAPI"),
            ("JWT_AUDIENCE", "TodosOsClientes"));

        var act = async () => await AuthHelper.ObterTokenAsync(new HttpClient());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(act);
        ex.Message.Should().Contain("invalid JWT format");
    }

    [Fact]
    public async Task ObterTokenAsync_ComAuthBaseUrlERetornoTokenValido_DeveRetornarToken()
    {
        var token = CriarTokenValido();
        using var server = FakeAuthServer.Start($"{{\"token\":\"{token}\"}}");
        using var scope = new EnvironmentScope(
            ("AUTH_BASE_URL", server.BaseUrl),
            ("JWT_ISSUER", "FacShopAPI"),
            ("JWT_AUDIENCE", "TodosOsClientes"));

        var result = await AuthHelper.ObterTokenAsync(new HttpClient());

        result.Should().Be(token);
    }

    private static string CriarTokenValido()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("MinhaChaveSuperSecretaDePeloMenos32BytesAki123!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: "FacShopAPI",
            audience: "TodosOsClientes",
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private sealed class EnvironmentScope : IDisposable
    {
        private readonly Dictionary<string, string?> _oldValues = new();

        public EnvironmentScope(params (string key, string? value)[] variables)
        {
            foreach (var (key, value) in variables)
            {
                _oldValues[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        public void Dispose()
        {
            foreach (var entry in _oldValues)
                Environment.SetEnvironmentVariable(entry.Key, entry.Value);
        }
    }

    private sealed class FakeAuthServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Task _serverTask;

        public string BaseUrl { get; }

        private FakeAuthServer(string responseBody, int port)
        {
            BaseUrl = $"http://127.0.0.1:{port}";
            _listener = new HttpListener();
            _listener.Prefixes.Add($"{BaseUrl}/");
            _listener.Start();

            _serverTask = Task.Run(async () =>
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.ContentType = "application/json";
                    var bytes = Encoding.UTF8.GetBytes(responseBody);
                    context.Response.ContentLength64 = bytes.Length;
                    await context.Response.OutputStream.WriteAsync(bytes);
                    context.Response.OutputStream.Close();
                }
                catch (HttpListenerException)
                {
                    // Listener stopped during test cleanup.
                }
            });
        }

        public static FakeAuthServer Start(string responseBody)
        {
            var port = GetFreePort();
            return new FakeAuthServer(responseBody, port);
        }

        private static int GetFreePort()
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public void Dispose()
        {
            _listener.Stop();
            _listener.Close();
            _serverTask.GetAwaiter().GetResult();
        }
    }
}
