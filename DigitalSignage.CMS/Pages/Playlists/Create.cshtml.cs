using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.Playlists;

public class CreateModel : PageModel
{
    private readonly AppDbContext _context;

    public CreateModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Playlist Playlist { get; set; } = new();

    [BindProperty]
    public List<int> SelectedContentIds { get; set; } = new();

    public List<SelectListItem> ContentItemOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadContentItemOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadContentItemOptionsAsync();
            return Page();
        }

        Playlist.ContentIds = SelectedContentIds;
        Playlist.Status = PlaylistStatus.Approved; // created directly by Admin/Manager - no separate approval needed

        _context.Playlists.Add(Playlist);
        await _context.SaveChangesAsync();

        return RedirectToPage("Index");
    }

    private async Task LoadContentItemOptionsAsync()
    {
        var items = await _context.ContentItems.OrderBy(c => c.Name).ToListAsync();
        ContentItemOptions = items
            .Select(c => new SelectListItem($"{c.Name} ({c.ContentType})", c.Id.ToString()))
            .ToList();
    }
}
