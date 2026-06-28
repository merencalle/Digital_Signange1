using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.CMS.Security;

namespace DigitalSignage.CMS.Pages.Users;

public class CreateModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly AppDbContext _context;

    public CreateModel(UserManager<IdentityUser> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [BindProperty]
    public string UserName { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string Role { get; set; } = Security.Roles.User;

    [BindProperty]
    public int? OwnedDeviceId { get; set; }

    public List<SelectListItem> RoleOptions { get; set; } = Security.Roles.All
        .Select(r => new SelectListItem(r, r))
        .ToList();

    public List<SelectListItem> UnownedDeviceOptions { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadUnownedDevicesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Username and password are required.";
            await LoadUnownedDevicesAsync();
            return Page();
        }

        var user = new IdentityUser { UserName = UserName, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, Password);

        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
            await LoadUnownedDevicesAsync();
            return Page();
        }

        await _userManager.AddToRoleAsync(user, Role);
        await _userManager.AddClaimAsync(user, new Claim(SentinelClaims.MustChangePassword, "true"));

        if (Role == Security.Roles.User && OwnedDeviceId is not null)
        {
            var device = await _context.Devices.FindAsync(OwnedDeviceId.Value);
            if (device is not null)
            {
                device.OwnerUserId = user.Id;
                await _context.SaveChangesAsync();
            }
        }

        return RedirectToPage("Index");
    }

    private async Task LoadUnownedDevicesAsync()
    {
        var devices = await _context.Devices.Where(d => d.OwnerUserId == null).OrderBy(d => d.Name).ToListAsync();
        UnownedDeviceOptions = devices.Select(d => new SelectListItem(d.Name, d.Id.ToString())).ToList();
    }
}
