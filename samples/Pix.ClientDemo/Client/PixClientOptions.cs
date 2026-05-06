namespace Pix.ClientDemo.Client;

public sealed class PixClientOptions
{
    public const string SectionName = "PixClient";

    public string BaseUrl { get; set; } = "https://localhost:5099";
    public string ClientId { get; set; } = "pix-demo-client";
    public string ClientSecret { get; set; } = "pix-demo-secret";
    public string Scope { get; set; } = "pix.cobranca pix.devolucao";

    public string? CertificateDirectory { get; set; }
    public string CertificatePassword { get; set; } = "pix-demo-123";
    public string ClientCertificateFileName { get; set; } = "client.pfx";
    public string CaCertificateFileName { get; set; } = "ca.cer";
    public string ExpectedServerDnsName { get; set; } = "localhost";
}
