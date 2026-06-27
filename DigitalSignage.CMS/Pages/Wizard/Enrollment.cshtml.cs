using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.CMS.Security;
using DigitalSignage.Shared.Dtos;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.Wizard;

public class EnrollmentModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly CertificateService _certService;

    public EnrollmentModel(AppDbContext context, CertificateService certService)
    {
        _context = context;
        _certService = certService;
    }

    [BindProperty(SupportsGet = true)]
    public string Scenario { get; set; } = "airgapped";

    [BindProperty(SupportsGet = true)]
    public string Variant { get; set; } = "standard";

    [BindProperty(SupportsGet = true)]
    public int? DeviceId { get; set; }

    [BindProperty]
    public string NewDeviceName { get; set; } = string.Empty;

    [BindProperty]
    public string NewDeviceLocation { get; set; } = string.Empty;

    public List<Device> PendingDevices { get; set; } = new();
    public Device? SelectedDevice { get; set; }
    public string CmsBaseUrl { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreatePendingAsync()
    {
        var device = new Device
        {
            Name = string.IsNullOrWhiteSpace(NewDeviceName) ? "New Player" : NewDeviceName,
            Location = NewDeviceLocation,
            DeviceType = "WindowsPlayer",
            Status = "Offline",
            LastHeartbeat = DateTime.UtcNow,
            IsPaired = false,
            PairingSecret = GenerateSecret()
        };

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        return RedirectToPage(new { Scenario, Variant, DeviceId = device.Id });
    }

    public async Task<IActionResult> OnPostRegenerateSecretAsync(int id)
    {
        var device = await _context.Devices.FindAsync(id);
        if (device is not null && !device.IsPaired)
        {
            device.PairingSecret = GenerateSecret();
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { Scenario, Variant, DeviceId = id });
    }

    public async Task<IActionResult> OnGetDownloadPackageAsync(int id)
    {
        var device = await _context.Devices.FindAsync(id);
        if (device is null || device.IsPaired || string.IsNullOrEmpty(device.PairingSecret))
        {
            return NotFound();
        }

        var package = new EnrollmentPackage
        {
            CmsBaseUrl = ComputeCmsBaseUrl(),
            PairingSecret = device.PairingSecret,
            PinnedCertThumbprint = Variant == "secure" ? _certService.GetCurrentCertificateInfo()?.Thumbprint : null
        };

        var json = JsonSerializer.Serialize(package, new JsonSerializerOptions { WriteIndented = true });
        return File(Encoding.UTF8.GetBytes(json), "application/json", $"sentinel-enrollment-{device.Id}.json");
    }

    private async Task LoadAsync()
    {
        PendingDevices = await _context.Devices.Where(d => !d.IsPaired).OrderBy(d => d.Name).ToListAsync();
        if (DeviceId is not null)
        {
            SelectedDevice = await _context.Devices.FindAsync(DeviceId.Value);
        }
        CmsBaseUrl = ComputeCmsBaseUrl();
    }

    private string ComputeCmsBaseUrl() => $"https://{Request.Host.Host}:5110";

    private static string GenerateSecret() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
}
