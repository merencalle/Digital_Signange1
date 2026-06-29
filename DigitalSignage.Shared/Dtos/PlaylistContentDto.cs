using DigitalSignage.Shared.Models;

namespace DigitalSignage.Shared.Dtos;

public class PlaylistContentItemDto
{
    public ContentItem ContentItem { get; set; } = new();
    public int DurationSeconds { get; set; }
}

public class PlaylistContentDto
{
    public int PlaylistId { get; set; }
    public string PlaylistName { get; set; } = string.Empty;
    public List<PlaylistContentItemDto> Items { get; set; } = new();
}
