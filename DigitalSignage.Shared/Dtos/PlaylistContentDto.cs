using DigitalSignage.Shared.Models;

namespace DigitalSignage.Shared.Dtos;

public class PlaylistContentDto
{
    public int PlaylistId { get; set; }
    public string PlaylistName { get; set; } = string.Empty;
    public List<ContentItem> Items { get; set; } = new();
}
