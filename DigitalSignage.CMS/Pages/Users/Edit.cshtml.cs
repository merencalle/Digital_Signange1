using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;

namespace DigitalSignage.CMS.Pages.Users;

public class EditModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly AppDbContext _context;

    public EditModel(UserManager<IdentityUser> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [BindProperty]
    public string Id { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    [BindProperty]
    public string Role { get; set; } = Security.Roles.User;

    [BindProperty]
    public int? OwnedDeviceId { get; set; }

    [BindProperty]
    public string? NewPassword { get; set; }

    public List<SelectListItem> RoleOptions { get; set; } = Security.Roles.All
        .Select(r => new SelectListItem(r, r))
        .ToList();

    public List<SelectListItem> DeviceOptions { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        Id = user.Id;
        UserName = user.UserName ?? string.Empty;
        Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? Security.Roles.User;

        var owned = await _context.Devices.FirstOrDefaultAsync(d => d.OwnerUserId == user.Id);
        OwnedDeviceId = owned?.Id;

        await LoadDeviceOptionsAsync(owned?.Id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.FindByIdAsync(Id);
        if (user is null)
        {
            return NotFound();
        }

        UserName = user.UserName ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, NewPassword);
            if (!resetResult.Succeeded)
            {
                ErrorMessage = string.Join(" ", resetResult.Errors.Select(e => e.Description));
                await LoadDeviceOptionsAsync(OwnedDeviceId);
                return Page();
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }
        await _userManager.AddToRoleAsync(user, Role);

        var currentlyOwned = await _context.Devices.Where(d => d.OwnerUserId == user.Id).ToListAsync();
        foreach (var device in currentlyOwned)
        {
            device.OwnerUserId = null;
        }

        if (Role == Security.Roles.User && OwnedDeviceId is not null)
        {
            var newDevice = await _context.Devices.FindAsync(OwnedDeviceId.Value);
            if (newDevice is not null)
            {
                newDevice.OwnerUserId = user.Id;
            }
        }

        await _context.SaveChangesAsync();

        Message = "User updated.";
        await LoadDeviceOptionsAsync(OwnedDeviceId);
        return Page();
    }

    private async Task LoadDeviceOptionsAsync(int? currentlyOwnedId)
    {
        var devices = await _context.Devices
            .Where(d => d.OwnerUserId == null || d.Id == currentlyOwnedId)
            .OrderBy(d => d.Name)
            .ToListAsync();
        DeviceOptions = devices.Select(d => new SelectListItem(d.Name, d.Id.ToString())).ToList();
    }
}
