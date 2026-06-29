using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.MyPlayer;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public IndexModel(AppDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Device? MyDevice { get; set; }
    public Playlist? MyPlaylist { get; set; }

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        MyDevice = await _context.Devices.FirstOrDefaultAsync(d => d.OwnerUserId == userId);

        if (MyDevice?.PlaylistId is not null)
        {
            MyPlaylist = await _context.Playlists.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == MyDevice.PlaylistId.Value);
        }
    }
}
