using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.Approvals;

public class PendingPlaylistRow
{
    public Playlist Playlist { get; set; } = new();
    public string SubmittedByName { get; set; } = "(unknown)";
    public List<PlaylistItem> Items { get; set; } = new();
}

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(AppDbContext context, UserManager<IdentityUser> userManager, ILogger<IndexModel> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public List<PendingPlaylistRow> Pending { get; set; } = new();

    public async Task OnGetAsync()
    {
        var playlists = await _context.Playlists
            .Include(p => p.Items.OrderBy(i => i.Order))
            .ThenInclude(i => i.ContentItem)
            .Where(p => p.Status == PlaylistStatus.PendingApproval)
            .OrderBy(p => p.SubmittedAt)
            .ToListAsync();

        foreach (var playlist in playlists)
        {
            var submitter = playlist.SubmittedByUserId is null
                ? null
                : await _userManager.FindByIdAsync(playlist.SubmittedByUserId);

            Pending.Add(new PendingPlaylistRow
            {
                Playlist = playlist,
                SubmittedByName = submitter?.UserName ?? "(unknown)",
                Items = playlist.Items.OrderBy(i => i.Order).ToList()
            });
        }
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var playlist = await _context.Playlists.FindAsync(id);
        if (playlist is null || playlist.Status != PlaylistStatus.PendingApproval)
        {
            return RedirectToPage();
        }

        playlist.Status = PlaylistStatus.Approved;
        playlist.ApprovedByUserId = _userManager.GetUserId(User);
        playlist.ApprovedAt = DateTime.UtcNow;
        playlist.RejectionReason = null;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Playlist '{Name}' (id {Id}) approved by {ApproverId}", playlist.Name, playlist.Id, playlist.ApprovedByUserId);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, string? reason)
    {
        var playlist = await _context.Playlists.FindAsync(id);
        if (playlist is null || playlist.Status != PlaylistStatus.PendingApproval)
        {
            return RedirectToPage();
        }

        playlist.Status = PlaylistStatus.Rejected;
        playlist.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "No reason given." : reason;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Playlist '{Name}' (id {Id}) rejected by {ApproverId}: {Reason}",
            playlist.Name, playlist.Id, _userManager.GetUserId(User), playlist.RejectionReason);

        return RedirectToPage();
    }
}
