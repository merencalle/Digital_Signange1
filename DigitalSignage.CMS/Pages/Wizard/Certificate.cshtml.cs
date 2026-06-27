using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DigitalSignage.CMS.Security;

namespace DigitalSignage.CMS.Pages.Wizard;

public class CertificateModel : PageModel
{
    private readonly CertificateService _certService;
    private readonly ILogger<CertificateModel> _logger;

    public CertificateModel(CertificateService certService, ILogger<CertificateModel> logger)
    {
        _certService = certService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string Scenario { get; set; } = "airgapped";

    [BindProperty(SupportsGet = true)]
    public string Variant { get; set; } = "standard";

    [BindProperty]
    public string CommonName { get; set; } = string.Empty;

    [BindProperty]
    public int ValidityYears { get; set; } = 10;

    [BindProperty]
    public IFormFile? UploadedCertFile { get; set; }

    [BindProperty]
    public string? UploadedCertPassword { get; set; }

    [BindProperty]
    public bool RequireMutualTls { get; set; }

    public CertificateInfo? CurrentCert { get; set; }
    public string? PendingCsr { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        Load();
        if (string.IsNullOrWhiteSpace(CommonName))
        {
            CommonName = Environment.MachineName;
        }
    }

    public IActionResult OnPostGenerateSelfSigned()
    {
        try
        {
            var info = _certService.GenerateSelfSigned(string.IsNullOrWhiteSpace(CommonName) ? Environment.MachineName : CommonName, ValidityYears);
            _logger.LogWarning(
                "New self-signed CMS certificate generated for '{CommonName}' (valid {Years}y), thumbprint {Thumbprint}. Restart Sentinel to apply it.",
                CommonName, ValidityYears, info.Thumbprint);
            Message = "Certificate generated. Restart Sentinel for it to take effect, then download the trust files below for your Player machines.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        Load();
        return Page();
    }

    public IActionResult OnPostGenerateCsr()
    {
        PendingCsr = _certService.GenerateCsr(string.IsNullOrWhiteSpace(CommonName) ? Environment.MachineName : CommonName);
        Message = "CSR generated. Download it, submit it to your CA team, then upload the signed certificate they return below.";
        Load();
        return Page();
    }

    public IActionResult OnGetDownloadCsr()
    {
        var csr = _certService.GetPendingCsr();
        return csr is null ? NotFound() : File(Encoding.UTF8.GetBytes(csr), "application/x-pem-file", "sentinel-cms.csr");
    }

    public async Task<IActionResult> OnPostCompleteCsrAsync()
    {
        if (UploadedCertFile is null)
        {
            ErrorMessage = "Choose the signed certificate file your CA returned.";
            Load();
            return Page();
        }

        using var ms = new MemoryStream();
        await UploadedCertFile.CopyToAsync(ms);

        try
        {
            var info = _certService.CompleteCsr(ms.ToArray());
            _logger.LogWarning(
                "CMS certificate installed from CA-signed upload, thumbprint {Thumbprint}. Restart Sentinel to apply it.",
                info.Thumbprint);
            Message = "Signed certificate installed. Restart Sentinel for it to take effect.";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Could not install that certificate: " + ex.Message;
        }

        Load();
        return Page();
    }

    public async Task<IActionResult> OnPostUploadPfxAsync()
    {
        if (UploadedCertFile is null)
        {
            ErrorMessage = "Choose a certificate file (.pfx).";
            Load();
            return Page();
        }

        using var ms = new MemoryStream();
        await UploadedCertFile.CopyToAsync(ms);

        try
        {
            var info = _certService.ImportPfx(ms.ToArray(), UploadedCertPassword);
            _logger.LogWarning(
                "CMS certificate imported from uploaded PFX, thumbprint {Thumbprint}. Restart Sentinel to apply it.",
                info.Thumbprint);
            Message = "Certificate imported. Restart Sentinel for it to take effect.";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Could not import that file: " + ex.Message;
        }

        Load();
        return Page();
    }

    public IActionResult OnGetDownloadCer()
    {
        if (!File.Exists(_certService.CerPath))
        {
            return NotFound();
        }

        return File(_certService.GetPublicCertBytes(), "application/x-x509-ca-cert", "sentinel-cms.cer");
    }

    public IActionResult OnGetDownloadTrustScript()
    {
        var script = CertificateService.GenerateTrustInstallScript("sentinel-cms.cer");
        return File(Encoding.UTF8.GetBytes(script), "text/plain", "Install-TrustedCert.ps1");
    }

    private void Load()
    {
        CurrentCert = _certService.GetCurrentCertificateInfo();
        PendingCsr = _certService.GetPendingCsr();
    }
}
