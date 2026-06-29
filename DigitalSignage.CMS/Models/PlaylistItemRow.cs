namespace DigitalSignage.CMS.Models;

public class PlaylistItemRow
{
    public int ContentItemId { get; set; }
    public string ContentItemName { get; set; } = string.Empty;
    public string ContentItemType { get; set; } = string.Empty;
    public bool Selected { get; set; }
    public int Order { get; set; }
    public int DurationSeconds { get; set; } = 8;
}
