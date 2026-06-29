namespace DigitalSignage.Shared.Models;

public static class PlaylistStatus
{
    public const string Draft = "Draft";
    public const string PendingApproval = "PendingApproval";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

public class Playlist
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<PlaylistItem> Items { get; set; } = new();

    public string Status { get; set; } = PlaylistStatus.Draft;
    public string? SubmittedByUserId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    // All optional; null/empty means "no restriction" for that dimension.
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? DaysOfWeek { get; set; } // comma-separated DayOfWeek values, e.g. "Monday,Tuesday,Wednesday"
    public TimeOnly? DailyStartTime { get; set; }
    public TimeOnly? DailyEndTime { get; set; }

    public bool IsActiveAt(DateTime when)
    {
        var date = DateOnly.FromDateTime(when);
        if (StartDate is not null && date < StartDate) return false;
        if (EndDate is not null && date > EndDate) return false;

        if (!string.IsNullOrWhiteSpace(DaysOfWeek))
        {
            var allowedDays = DaysOfWeek.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (!allowedDays.Contains(when.DayOfWeek.ToString(), StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        var time = TimeOnly.FromDateTime(when);
        if (DailyStartTime is not null && time < DailyStartTime) return false;
        if (DailyEndTime is not null && time > DailyEndTime) return false;

        return true;
    }
}

public class PlaylistItem
{
    public int Id { get; set; }
    public int PlaylistId { get; set; }
    public Playlist Playlist { get; set; } = null!;
    public int ContentItemId { get; set; }
    public ContentItem ContentItem { get; set; } = null!;
    public int Order { get; set; }
    public int DurationSeconds { get; set; } = 8; // applies to images; videos play their natural length unless overridden
}
