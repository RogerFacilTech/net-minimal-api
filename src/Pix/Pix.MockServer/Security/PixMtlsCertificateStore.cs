using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Pix.MockServer.Security;

public sealed record PixMtlsCertificateBundle(
    string CertDirectory,
    string CaCertificatePath,
    string ServerCertificatePath,
    string ClientCertificatePath,
    string CertificatePassword,
    string ExpectedClientSubject);

public static class PixMtlsCertificateStore
{
    private const string CaCerFile = "ca.cer";
    private const string CaPfxFile = "ca.pfx";
    private const string ServerPfxFile = "server.pfx";
    private const string ClientPfxFile = "client.pfx";

    public static PixMtlsCertificateBundle Ensure(string contentRootPath, string password, string expectedClientSubject)
    {
        var repoRoot = ResolveRepositoryRoot(contentRootPath);
        var certDirectory = Path.Combine(repoRoot, "certs", "pix");
        Directory.CreateDirectory(certDirectory);

        var caCerPath = Path.Combine(certDirectory, CaCerFile);
        var caPfxPath = Path.Combine(certDirectory, CaPfxFile);
        var serverPfxPath = Path.Combine(certDirectory, ServerPfxFile);
        var clientPfxPath = Path.Combine(certDirectory, ClientPfxFile);

        if (!File.Exists(caCerPath) || !File.Exists(caPfxPath) || !File.Exists(serverPfxPath) || !File.Exists(clientPfxPath))
        {
            GenerateCertificates(caCerPath, caPfxPath, serverPfxPath, clientPfxPath, password, expectedClientSubject);
        }

        return new PixMtlsCertificateBundle(
            certDirectory,
            caCerPath,
            serverPfxPath,
            clientPfxPath,
            password,
            expectedClientSubject);
    }

    public static bool ValidateClientCertificate(X509Certificate2? certificate, string caCertificatePath, string expectedClientSubject)
    {
        if (certificate is null)
        {
            return false;
        }

        using var caCert = X509CertificateLoader.LoadCertificateFromFile(caCertificatePath);
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

        var simpleName = certificate.GetNameInfo(X509NameType.SimpleName, false);
        return string.Equals(simpleName, expectedClientSubject, StringComparison.Ordinal);
    }

    private static void GenerateCertificates(
        string caCerPath,
        string caPfxPath,
        string serverPfxPath,
        string clientPfxPath,
        string password,
        string expectedClientSubject)
    {
        var now = DateTimeOffset.UtcNow;

        using var caKey = RSA.Create(4096);
        var caRequest = new CertificateRequest("CN=PixMock-CA", caKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        caRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(caRequest.PublicKey, false));
        caRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));

        using var caCert = caRequest.CreateSelfSigned(now.AddDays(-1), now.AddYears(10));

        using var serverKey = RSA.Create(2048);
        var serverRequest = new CertificateRequest("CN=localhost", serverKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
        serverRequest.CertificateExtensions.Add(sanBuilder.Build());
        serverRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        serverRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(serverRequest.PublicKey, false));
        serverRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        serverRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new("1.3.6.1.5.5.7.3.1") },
            true));

        var serverSerial = RandomNumberGenerator.GetBytes(16);
        using var serverCertWithoutKey = serverRequest.Create(caCert, now.AddDays(-1), now.AddYears(2), serverSerial);
        using var serverCert = serverCertWithoutKey.CopyWithPrivateKey(serverKey);

        using var clientKey = RSA.Create(2048);
        var clientRequest = new CertificateRequest($"CN={expectedClientSubject}", clientKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        clientRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        clientRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(clientRequest.PublicKey, false));
        clientRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        clientRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new("1.3.6.1.5.5.7.3.2") },
            true));

        var clientSerial = RandomNumberGenerator.GetBytes(16);
        using var clientCertWithoutKey = clientRequest.Create(caCert, now.AddDays(-1), now.AddYears(2), clientSerial);
        using var clientCert = clientCertWithoutKey.CopyWithPrivateKey(clientKey);

        File.WriteAllBytes(caCerPath, caCert.Export(X509ContentType.Cert));
        File.WriteAllBytes(caPfxPath, caCert.Export(X509ContentType.Pkcs12, password));
        File.WriteAllBytes(serverPfxPath, serverCert.Export(X509ContentType.Pkcs12, password));
        File.WriteAllBytes(clientPfxPath, clientCert.Export(X509ContentType.Pkcs12, password));
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
