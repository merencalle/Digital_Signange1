using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.Devices;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _context;

    public DeleteModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
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

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var device = await _context.Devices.FindAsync(id);
        if (device is not null)
        {
            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("Index");
    }
}
