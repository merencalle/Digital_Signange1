using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public int OnlineDeviceCount { get; set; }
    public int OfflineDeviceCount { get; set; }
    public int ContentItemCount { get; set; }
    public int PlaylistCount { get; set; }
    public IList<Device> RecentDevices { get; set; } = new List<Device>();

    public async Task OnGetAsync()
    {
        OnlineDeviceCount = await _context.Devices.CountAsync(d => d.Status == "Online");
        OfflineDeviceCount = await _context.Devices.CountAsync(d => d.Status != "Online");
        ContentItemCount = await _context.ContentItems.CountAsync();
        PlaylistCount = await _context.Playlists.CountAsync();
        RecentDevices = await _context.Devices
            .OrderByDescending(d => d.LastHeartbeat)
            .Take(5)
            .ToListAsync();
    }
}
