using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.Devices;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public Device Device { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var device = await _context.Devices.FindAsync(id);
        if (device is null)
        {
            return NotFound();
        }

        Device = device;
        return Page();
    }
}
