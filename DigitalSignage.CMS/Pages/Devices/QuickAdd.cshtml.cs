using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DigitalSignage.CMS.Data;
using DigitalSignage.CMS.Security;
using DigitalSignage.CMS.Services;
using DigitalSignage.Shared.Dtos;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.Devices;

public class QuickAddModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly CertificateService _certService;
    private readonly PlayerPackager _packager;
    private readonly ILogger<QuickAddModel> _logger;

    public QuickAddModel(AppDbContext context, CertificateService certService, PlayerPackager packager, ILogger<QuickAddModel> logger)
    {
        _context = context;
        _certService = certService;
        _packager = packager;
        _logger = logger;
    }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string Location { get; set; } = string.Empty;

    public bool PlayerBuildAvailable => _packager.IsPlayerBuildAvailable;

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Give the player a name.";
            return Page();
        }

        var device = new Device
        {
            Name = Name,
            Location = Location,
            DeviceType = "WindowsPlayer",
            Status = "Offline",
            LastHeartbeat = DateTime.UtcNow,
            IsPaired = false,
            PairingSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()
        };

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        var enrollment = new EnrollmentPackage
        {
            CmsBaseUrl = $"https://{Request.Host.Host}:5110",
            PairingSecret = device.PairingSecret!,
            PinnedCertThumbprint = _certService.GetCurrentCertificateInfo()?.Thumbprint
        };

        byte[] zipBytes;
        try
        {
            zipBytes = _packager.CreatePackage(enrollment);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }

        _logger.LogInformation("Player package generated for new device '{Name}' (id {Id})", device.Name, device.Id);

        var fileName = $"sentinel-player-{device.Name.Replace(' ', '-')}.zip";
        return File(zipBytes, "application/zip", fileName);
    }
}
