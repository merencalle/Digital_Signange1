namespace DigitalSignage.CMS.Models;

public class LogEntry
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? RequestPath { get; set; }
    public string? RemoteIp { get; set; }
    public int? StatusCode { get; set; }
    public long? DurationMs { get; set; }
}
