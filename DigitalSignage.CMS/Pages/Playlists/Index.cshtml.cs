using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.Playlists;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public IList<Playlist> Playlists { get; set; } = new List<Playlist>();

    public async Task OnGetAsync()
    {
        Playlists = await _context.Playlists.OrderBy(p => p.Name).ToListAsync();
    }
}
