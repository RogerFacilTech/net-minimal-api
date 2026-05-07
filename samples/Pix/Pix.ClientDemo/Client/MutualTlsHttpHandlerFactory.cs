using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Pix.ClientDemo.Client;

public static class MutualTlsHttpHandlerFactory
{
    public static HttpClientHandler Create(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<PixClientOptions>>().Value;
        var env = services.GetRequiredService<IHostEnvironment>();

        var certDirectory = string.IsNullOrWhiteSpace(options.CertificateDirectory)
            ? Path.Combine(ResolveRepositoryRoot(env.ContentRootPath), "certs", "pix")
            : options.CertificateDirectory;

        var clientCertPath = Path.Combine(certDirectory, options.ClientCertificateFileName);
        var caCertPath = Path.Combine(certDirectory, options.CaCertificateFileName);

        if (!File.Exists(clientCertPath) || !File.Exists(caCertPath))
        {
            throw new InvalidOperationException(
                $"Certificados mTLS não encontrados em '{certDirectory}'. Inicie primeiro o Pix.MockServer para gerar os certificados.");
        }

        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
                ValidateServerCertificate(certificate, caCertPath, options.ExpectedServerDnsName)
        };

        var clientCertificate = X509CertificateLoader.LoadPkcs12FromFile(
            clientCertPath,
            options.CertificatePassword,
            X509KeyStorageFlags.Exportable);
        handler.ClientCertificates.Add(clientCertificate);

        return handler;
    }

    private static bool ValidateServerCertificate(X509Certificate2? certificate, string caCertPath, string expectedServerDnsName)
    {
        if (certificate is null)
        {
            return false;
        }

        using var caCert = X509CertificateLoader.LoadCertificateFromFile(caCertPath);
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(caCert);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

        var validChain = chain.Build(certificate);
        if (!validChain)
        {
            return false;
        }

        var dnsName = certificate.GetNameInfo(X509NameType.DnsName, false);
        return string.Equals(dnsName, expectedServerDnsName, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveRepositoryRoot(string contentRootPath)
    {
        var firstTry = FindRepositoryRoot(contentRootPath);
        if (firstTry is not null)
        {
            return firstTry;
        }

        var secondTry = FindRepositoryRoot(Directory.GetCurrentDirectory());
        if (secondTry is not null)
        {
            return secondTry;
        }

        return contentRootPath;
    }

    private static string? FindRepositoryRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "FacShopAPI.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
