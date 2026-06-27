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
    public List<int> ContentIds { get; set; } = new();
    public string? ScheduleJson { get; set; } // For future scheduling

    public string Status { get; set; } = PlaylistStatus.Draft;
    public string? SubmittedByUserId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
}