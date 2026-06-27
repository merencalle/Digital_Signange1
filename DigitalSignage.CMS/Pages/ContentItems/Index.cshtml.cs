using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.ContentItems;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public IList<ContentItem> ContentItems { get; set; } = new List<ContentItem>();

    public async Task OnGetAsync()
    {
        ContentItems = await _context.ContentItems.OrderBy(c => c.Name).ToListAsync();
    }
}
