using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.CMS.Models;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.Playlists;

public class CreateModel : PageModel
{
    private static readonly string[] AllDays =
        { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

    private readonly AppDbContext _context;

    public CreateModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Playlist Playlist { get; set; } = new();

    [BindProperty]
    public List<PlaylistItemRow> Rows { get; set; } = new();

    [BindProperty]
    public List<string> SelectedDays { get; set; } = new();

    public string[] DayOptions => AllDays;

    public async Task OnGetAsync()
    {
        await LoadRowsAsync(selectedIds: new());
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadRowsAsync(Rows.Where(r => r.Selected).Select(r => r.ContentItemId).ToHashSet());
            return Page();
        }

        Playlist.Items = Rows
            .Where(r => r.Selected)
            .OrderBy(r => r.Order)
            .Select(r => new PlaylistItem
            {
                ContentItemId = r.ContentItemId,
                Order = r.Order,
                DurationSeconds = r.DurationSeconds > 0 ? r.DurationSeconds : 8
            })
            .ToList();
        Playlist.DaysOfWeek = SelectedDays.Count == 0 ? null : string.Join(",", SelectedDays);
        Playlist.Status = PlaylistStatus.Approved; // created directly by Admin/Manager - no separate approval needed

        _context.Playlists.Add(Playlist);
        await _context.SaveChangesAsync();

        return RedirectToPage("Index");
    }

    private async Task LoadRowsAsync(HashSet<int> selectedIds)
    {
        var items = await _context.ContentItems.OrderBy(c => c.Name).ToListAsync();
        Rows = items.Select((c, index) => new PlaylistItemRow
        {
            ContentItemId = c.Id,
            ContentItemName = c.Name,
            ContentItemType = c.ContentType,
            Selected = selectedIds.Contains(c.Id),
            Order = index,
            DurationSeconds = 8
        }).ToList();
    }
}
