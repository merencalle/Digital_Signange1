using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.ContentItems;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _context;

    public DeleteModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ContentItem ContentItem { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await _context.ContentItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        ContentItem = item;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var item = await _context.ContentItems.FindAsync(id);
        if (item is not null)
        {
            _context.ContentItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("Index");
    }
}
