using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DigitalSignage.CMS.Security;

public class CertificateInfo
{
    public string Subject { get; set; } = string.Empty;
    public string Thumbprint { get; set; } = string.Empty;
    public DateTime NotBefore { get; set; }
    public DateTime NotAfter { get; set; }
}

public class CertificateService
{
    private readonly string _certDirectory;

    public CertificateService(IWebHostEnvironment environment)
    {
        _certDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "certs");
        Directory.CreateDirectory(_certDirectory);
    }

    public string PfxPath => Path.Combine(_certDirectory, "cms.pfx");
    public string CerPath => Path.Combine(_certDirectory, "cms.cer");
    private string PendingKeyPath => Path.Combine(_certDirectory, "pending.key");
    private string PendingCsrPath => Path.Combine(_certDirectory, "pending.csr");

    public bool HasPendingCsr => File.Exists(PendingKeyPath);

    public CertificateInfo? GetCurrentCertificateInfo()
    {
        if (!File.Exists(PfxPath))
        {
            return null;
        }

        using var cert = new X509Certificate2(PfxPath, (string?)null, X509KeyStorageFlags.Exportable);
        return ToInfo(cert);
    }

    public CertificateInfo GenerateSelfSigned(string commonName, int validityYears)
    {
        using var rsa = RSA.Create(2048);
        var request = BuildRequest(commonName, rsa);

        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(validityYears));

        File.WriteAllBytes(PfxPath, cert.Export(X509ContentType.Pfx));
        File.WriteAllBytes(CerPath, cert.Export(X509ContentType.Cert));
        ClearPendingCsr();

        return ToInfo(cert);
    }

    public string GenerateCsr(string commonName)
    {
        using var rsa = RSA.Create(2048);
        var request = BuildRequest(commonName, rsa);

        var csrBytes = request.CreateSigningRequest();
        var csrPem = PemEncoding.WriteString("CERTIFICATE REQUEST", csrBytes);

        File.WriteAllBytes(PendingKeyPath, rsa.ExportPkcs8PrivateKey());
        File.WriteAllText(PendingCsrPath, csrPem);

        return csrPem;
    }

    public string? GetPendingCsr() => File.Exists(PendingCsrPath) ? File.ReadAllText(PendingCsrPath) : null;

    public CertificateInfo CompleteCsr(byte[] signedCertBytes)
    {
        if (!File.Exists(PendingKeyPath))
        {
            throw new InvalidOperationException("No pending CSR found - generate a CSR first.");
        }

        using var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(File.ReadAllBytes(PendingKeyPath), out _);

        using var signedCert = new X509Certificate2(signedCertBytes);
        using var certWithKey = signedCert.CopyWithPrivateKey(rsa);
        using var exportable = new X509Certificate2(certWithKey.Export(X509ContentType.Pfx));

        File.WriteAllBytes(PfxPath, certWithKey.Export(X509ContentType.Pfx));
        File.WriteAllBytes(CerPath, signedCertBytes);
        ClearPendingCsr();

        return ToInfo(exportable);
    }

    public CertificateInfo ImportPfx(byte[] pfxBytes, string? password)
    {
        using var cert = new X509Certificate2(pfxBytes, password, X509KeyStorageFlags.Exportable);
        File.WriteAllBytes(PfxPath, cert.Export(X509ContentType.Pfx));
        File.WriteAllBytes(CerPath, cert.Export(X509ContentType.Cert));
        ClearPendingCsr();
        return ToInfo(cert);
    }

    public byte[] GetPublicCertBytes() => File.ReadAllBytes(CerPath);

    public static string GenerateTrustInstallScript(string cerFileName)
    {
        const string template = """
            # Run this script AS ADMINISTRATOR on each Player machine.
            # It trusts the Sentinel CMS certificate so the Player can connect over HTTPS.
            $certPath = Join-Path $PSScriptRoot '__CER_FILE__'
            if (-not (Test-Path $certPath)) {
                Write-Error "Certificate file not found next to this script: $certPath"
                exit 1
            }
            Import-Certificate -FilePath $certPath -CertStoreLocation Cert:\LocalMachine\Root
            Write-Host "Sentinel CMS certificate trusted on this machine."
            """;
        return template.Replace("__CER_FILE__", cerFileName);
    }

    private static CertificateRequest BuildRequest(string commonName, RSA rsa)
    {
        var request = new CertificateRequest($"CN={commonName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: false));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, critical: false)); // Server Authentication

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName(commonName);
        if (commonName != "localhost")
        {
            sanBuilder.AddDnsName("localhost");
        }
        request.CertificateExtensions.Add(sanBuilder.Build());

        return request;
    }

    private void ClearPendingCsr()
    {
        if (File.Exists(PendingKeyPath))
        {
            File.Delete(PendingKeyPath);
        }
        if (File.Exists(PendingCsrPath))
        {
            File.Delete(PendingCsrPath);
        }
    }

    private static CertificateInfo ToInfo(X509Certificate2 cert) => new()
    {
        Subject = cert.Subject,
        Thumbprint = cert.Thumbprint,
        NotBefore = cert.NotBefore,
        NotAfter = cert.NotAfter
    };
}
