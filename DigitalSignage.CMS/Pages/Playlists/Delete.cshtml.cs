using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.Playlists;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _context;

    public DeleteModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Playlist Playlist { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var playlist = await _context.Playlists.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id);
        if (playlist is null)
        {
            return NotFound();
        }

        Playlist = playlist;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var playlist = await _context.Playlists.FindAsync(id);
        if (playlist is not null)
        {
            _context.Playlists.Remove(playlist);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("Index");
    }
}
