using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.Devices;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public IList<Device> Devices { get; set; } = new List<Device>();

    public async Task OnGetAsync()
    {
        Devices = await _context.Devices.OrderBy(d => d.Name).ToListAsync();
    }
}
