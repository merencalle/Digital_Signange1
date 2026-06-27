using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.Playlists;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public Playlist Playlist { get; set; } = new();

    public List<ContentItem> Items { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var playlist = await _context.Playlists.FindAsync(id);
        if (playlist is null)
        {
            return NotFound();
        }

        Playlist = playlist;
        Items = await _context.ContentItems
            .Where(c => playlist.ContentIds.Contains(c.Id))
            .ToListAsync();

        return Page();
    }
}
