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

    public List<PlaylistItem> Items { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var playlist = await _context.Playlists
            .Include(p => p.Items.OrderBy(i => i.Order))
            .ThenInclude(i => i.ContentItem)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (playlist is null)
        {
            return NotFound();
        }

        Playlist = playlist;
        Items = playlist.Items.OrderBy(i => i.Order).ToList();

        return Page();
    }
}
