using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.Devices;

public class CreateModel : PageModel
{
    private readonly AppDbContext _context;

    public CreateModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Device Device { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Device.Status = "Offline";
        Device.LastHeartbeat = DateTime.UtcNow;

        _context.Devices.Add(Device);
        await _context.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
