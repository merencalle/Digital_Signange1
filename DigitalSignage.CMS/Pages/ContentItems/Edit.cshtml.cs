using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.ContentItems;

public class EditModel : PageModel
{
    private readonly AppDbContext _context;

    public EditModel(AppDbContext context)
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

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var existing = await _context.ContentItems.FindAsync(ContentItem.Id);
        if (existing is null)
        {
            return NotFound();
        }

        // Only the fields this form edits - leaves FileHash untouched so the player cache stays valid.
        existing.Name = ContentItem.Name;
        existing.ContentType = ContentItem.ContentType;
        existing.FilePath = ContentItem.FilePath;
        existing.FileSize = ContentItem.FileSize;

        await _context.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
