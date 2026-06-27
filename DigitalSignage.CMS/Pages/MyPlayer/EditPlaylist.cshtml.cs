using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.MyPlayer;

public class EditPlaylistModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public EditPlaylistModel(AppDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public string PlaylistName { get; set; } = string.Empty;

    [BindProperty]
    public List<int> SelectedContentIds { get; set; } = new();

    public List<SelectListItem> ContentItemOptions { get; set; } = new();
    public Playlist? Playlist { get; set; }
    public Device? MyDevice { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        MyDevice = await GetMyDeviceAsync();
        if (MyDevice is null)
        {
            return RedirectToPage("Index");
        }

        if (MyDevice.PlaylistId is not null)
        {
            Playlist = await _context.Playlists.FindAsync(MyDevice.PlaylistId.Value);
        }

        PlaylistName = Playlist?.Name ?? $"{MyDevice.Name} Playlist";
        SelectedContentIds = Playlist?.ContentIds ?? new();

        await LoadContentItemOptionsAsync();
        return Page();
    }

    public Task<IActionResult> OnPostSaveDraftAsync() => SaveAsync(submit: false);

    public Task<IActionResult> OnPostSubmitAsync() => SaveAsync(submit: true);

    private async Task<IActionResult> SaveAsync(bool submit)
    {
        MyDevice = await GetMyDeviceAsync();
        if (MyDevice is null)
        {
            return RedirectToPage("Index");
        }

        var playlist = MyDevice.PlaylistId is not null
            ? await _context.Playlists.FindAsync(MyDevice.PlaylistId.Value)
            : null;

        if (playlist is null)
        {
            playlist = new Playlist { Name = PlaylistName };
            _context.Playlists.Add(playlist);
            await _context.SaveChangesAsync();
            MyDevice.PlaylistId = playlist.Id;
        }
        else if (playlist.Status == PlaylistStatus.PendingApproval)
        {
            ErrorMessage = "This playlist is currently pending approval and can't be edited until a Manager reviews it.";
            Playlist = playlist;
            PlaylistName = playlist.Name;
            SelectedContentIds = playlist.ContentIds;
            await LoadContentItemOptionsAsync();
            return Page();
        }

        playlist.Name = PlaylistName;
        playlist.ContentIds = SelectedContentIds;
        playlist.Status = submit ? PlaylistStatus.PendingApproval : PlaylistStatus.Draft;
        if (submit)
        {
            playlist.SubmittedByUserId = _userManager.GetUserId(User);
            playlist.SubmittedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        Message = submit ? "Submitted for approval." : "Draft saved.";
        Playlist = playlist;
        await LoadContentItemOptionsAsync();
        return Page();
    }

    private async Task<Device?> GetMyDeviceAsync()
    {
        var userId = _userManager.GetUserId(User);
        return await _context.Devices.FirstOrDefaultAsync(d => d.OwnerUserId == userId);
    }

    private async Task LoadContentItemOptionsAsync()
    {
        var items = await _context.ContentItems.OrderBy(c => c.Name).ToListAsync();
        ContentItemOptions = items.Select(c => new SelectListItem($"{c.Name} ({c.ContentType})", c.Id.ToString())).ToList();
    }
}
