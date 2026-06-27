using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.Devices;

public class EditModel : PageModel
{
    private readonly AppDbContext _context;

    public EditModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Device Device { get; set; } = new();

    public List<SelectListItem> PlaylistOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var device = await _context.Devices.FindAsync(id);
        if (device is null)
        {
            return NotFound();
        }

        Device = device;
        await LoadPlaylistOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadPlaylistOptionsAsync();
            return Page();
        }

        _context.Attach(Device).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        await _context.SaveChangesAsync();

        return RedirectToPage("Index");
    }

    private async Task LoadPlaylistOptionsAsync()
    {
        var playlists = await _context.Playlists.OrderBy(p => p.Name).ToListAsync();
        PlaylistOptions = playlists
            .Select(p => new SelectListItem(p.Name, p.Id.ToString()))
            .ToList();
    }
}
