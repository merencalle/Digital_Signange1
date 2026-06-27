using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;

namespace DigitalSignage.CMS.Pages.Users;

public class UserRow
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = "(none)";
    public string? OwnedDeviceName { get; set; }
}

public class IndexModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly AppDbContext _context;

    public IndexModel(UserManager<IdentityUser> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public List<UserRow> Users { get; set; } = new();

    public async Task OnGetAsync()
    {
        var devices = await _context.Devices
            .Where(d => d.OwnerUserId != null)
            .ToListAsync();

        foreach (var user in _userManager.Users.OrderBy(u => u.UserName).ToList())
        {
            var roles = await _userManager.GetRolesAsync(user);
            Users.Add(new UserRow
            {
                Id = user.Id,
                UserName = user.UserName ?? "(unnamed)",
                Role = roles.FirstOrDefault() ?? "(none)",
                OwnedDeviceName = devices.FirstOrDefault(d => d.OwnerUserId == user.Id)?.Name
            });
        }
    }
}
