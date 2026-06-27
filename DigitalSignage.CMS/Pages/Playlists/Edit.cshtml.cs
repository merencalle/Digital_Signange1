using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.Playlists;

public class EditModel : PageModel
{
    private readonly AppDbContext _context;

    public EditModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Playlist Playlist { get; set; } = new();

    [BindProperty]
    public List<int> SelectedContentIds { get; set; } = new();

    public List<SelectListItem> ContentItemOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var playlist = await _context.Playlists.FindAsync(id);
        if (playlist is null)
        {
            return NotFound();
        }

        Playlist = playlist;
        SelectedContentIds = playlist.ContentIds;
        await LoadContentItemOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadContentItemOptionsAsync();
            return Page();
        }

        var existing = await _context.Playlists.FindAsync(Playlist.Id);
        if (existing is null)
        {
            return NotFound();
        }

        // Edited directly by Admin/Manager - that IS the approval, so it stays/becomes Approved.
        // Leaves Submitted*/Approved*/RejectionReason audit fields untouched.
        existing.Name = Playlist.Name;
        existing.ContentIds = SelectedContentIds;
        existing.Status = PlaylistStatus.Approved;

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
