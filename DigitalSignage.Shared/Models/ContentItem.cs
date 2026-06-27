namespace DigitalSignage.Shared.Models;

public class ContentItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty; // Image, Video, HTML, Widget
    public long FileSize { get; set; }
    public DateTime UploadDate { get; set; }
    public string FileHash { get; set; } = string.Empty; // SHA256 of the stored file, used by players to skip re-downloading unchanged content
}