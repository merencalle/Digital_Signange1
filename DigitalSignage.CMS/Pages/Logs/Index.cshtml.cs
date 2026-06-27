using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.CMS.Models;

namespace DigitalSignage.CMS.Pages.Logs;

public class IndexModel : PageModel
{
    private const int PageSize = 50;

    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public IList<LogEntry> Entries { get; set; } = new List<LogEntry>();

    [BindProperty(SupportsGet = true)]
    public string? Level { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.LogEntries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(Level))
        {
            query = query.Where(l => l.Level == Level);
        }

        if (!string.IsNullOrWhiteSpace(Category))
        {
            query = query.Where(l => l.Category.Contains(Category));
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            query = query.Where(l => l.Message.Contains(Search));
        }

        TotalCount = await query.CountAsync();
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);

        Entries = await query
            .OrderByDescending(l => l.Id)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostClearAsync()
    {
        await _context.LogEntries.ExecuteDeleteAsync();
        return RedirectToPage();
    }
}
