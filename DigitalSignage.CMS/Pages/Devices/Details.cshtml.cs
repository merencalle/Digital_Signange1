using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.Devices;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public Device Device { get; set; } = new();

    public string PlaylistName { get; set; } = "None";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var device = await _context.Devices.FindAsync(id);
        if (device is null)
        {
            return NotFound();
        }

        Device = device;

        if (device.PlaylistId is not null)
        {
            var playlist = await _context.Playlists.FindAsync(device.PlaylistId.Value);
            if (playlist is not null)
            {
                PlaylistName = playlist.Name;
            }
        }

        return Page();
    }
}
