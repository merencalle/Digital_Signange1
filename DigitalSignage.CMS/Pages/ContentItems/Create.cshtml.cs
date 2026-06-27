using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DigitalSignage.CMS.Data;
using DigitalSignage.CMS.Services;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Pages.ContentItems;

public class CreateModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly MediaConversionService _conversionService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(AppDbContext context, MediaConversionService conversionService, IWebHostEnvironment environment, ILogger<CreateModel> logger)
    {
        _context = context;
        _conversionService = conversionService;
        _environment = environment;
        _logger = logger;
    }

    [BindProperty]
    public ContentItem ContentItem { get; set; } = new();

    [BindProperty]
    public IFormFile? UploadedFile { get; set; }

    public string? UploadError { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(ContentItem.Name))
        {
            UploadError = "Name is required.";
            return Page();
        }

        if (UploadedFile is null || UploadedFile.Length == 0)
        {
            UploadError = "Please choose a file to upload.";
            return Page();
        }

        var extension = Path.GetExtension(UploadedFile.FileName).ToLowerInvariant();
        var isImage = MediaConversionService.IsSupportedImage(extension);
        var isVideo = MediaConversionService.IsSupportedVideo(extension);

        if (!isImage && !isVideo)
        {
            UploadError = $"Unsupported file type '{extension}'. Supported: images (png, jpg, gif, bmp, webp, heic...) and video (mp4, mov, avi, mkv, wmv, webm).";
            return Page();
        }

        var mediaDirectory = Path.Combine(_environment.WebRootPath, "media");
        Directory.CreateDirectory(mediaDirectory);

        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");
        await using (var stream = System.IO.File.Create(tempPath))
        {
            await UploadedFile.CopyToAsync(stream);
        }

        try
        {
            string savedFileName;
            if (isImage)
            {
                savedFileName = await _conversionService.ProcessImageAsync(tempPath, extension, mediaDirectory);
                ContentItem.ContentType = "Image";
            }
            else
            {
                savedFileName = await _conversionService.ProcessVideoAsync(tempPath, extension, mediaDirectory);
                ContentItem.ContentType = "Video";
            }

            var savedFullPath = Path.Combine(mediaDirectory, savedFileName);
            ContentItem.FilePath = $"media/{savedFileName}";
            ContentItem.FileSize = new FileInfo(savedFullPath).Length;
            ContentItem.UploadDate = DateTime.UtcNow;
        }
        finally
        {
            System.IO.File.Delete(tempPath);
        }

        _context.ContentItems.Add(ContentItem);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Content uploaded: '{Name}' ({ContentType}) saved as {FilePath}, original extension '{OriginalExtension}', {FileSize} bytes",
            ContentItem.Name, ContentItem.ContentType, ContentItem.FilePath, extension, ContentItem.FileSize);

        return RedirectToPage("Index");
    }
}
