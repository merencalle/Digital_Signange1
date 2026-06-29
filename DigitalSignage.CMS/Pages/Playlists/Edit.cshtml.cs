using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.CMS.Models;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.Playlists;

public class EditModel : PageModel
{
    private static readonly string[] AllDays =
        { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

    private readonly AppDbContext _context;

    public EditModel(AppDbContext context)
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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var playlist = await _context.Playlists
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (playlist is null)
        {
            return NotFound();
        }

        Playlist = playlist;
        SelectedDays = string.IsNullOrWhiteSpace(playlist.DaysOfWeek)
            ? new()
            : playlist.DaysOfWeek.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();

        await LoadRowsAsync(playlist.Items.ToDictionary(i => i.ContentItemId));
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadRowsAsync(Rows.Where(r => r.Selected).ToDictionary(r => r.ContentItemId, r => new PlaylistItem { Order = r.Order, DurationSeconds = r.DurationSeconds }));
            return Page();
        }

        var existing = await _context.Playlists
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == Playlist.Id);
        if (existing is null)
        {
            return NotFound();
        }

        // Edited directly by Admin/Manager - that IS the approval, so it stays/becomes Approved.
        // Leaves Submitted*/Approved*/RejectionReason audit fields untouched.
        existing.Name = Playlist.Name;
        existing.Status = PlaylistStatus.Approved;
        existing.StartDate = Playlist.StartDate;
        existing.EndDate = Playlist.EndDate;
        existing.DailyStartTime = Playlist.DailyStartTime;
        existing.DailyEndTime = Playlist.DailyEndTime;
        existing.DaysOfWeek = SelectedDays.Count == 0 ? null : string.Join(",", SelectedDays);

        existing.Items.Clear();
        existing.Items.AddRange(Rows
            .Where(r => r.Selected)
            .OrderBy(r => r.Order)
            .Select(r => new PlaylistItem
            {
                ContentItemId = r.ContentItemId,
                Order = r.Order,
                DurationSeconds = r.DurationSeconds > 0 ? r.DurationSeconds : 8
            }));

        await _context.SaveChangesAsync();

        return RedirectToPage("Index");
    }

    private async Task LoadRowsAsync(Dictionary<int, PlaylistItem> existingByContentId)
    {
        var items = await _context.ContentItems.OrderBy(c => c.Name).ToListAsync();
        Rows = items.Select((c, index) =>
        {
            var existing = existingByContentId.GetValueOrDefault(c.Id);
            return new PlaylistItemRow
            {
                ContentItemId = c.Id,
                ContentItemName = c.Name,
                ContentItemType = c.ContentType,
                Selected = existing is not null,
                Order = existing?.Order ?? index,
                DurationSeconds = existing?.DurationSeconds ?? 8
            };
        }).ToList();
    }
}
