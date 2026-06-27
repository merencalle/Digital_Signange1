namespace DigitalSignage.CMS.Logging;

public record LogEntryMessage(
    DateTime Timestamp,
    string Level,
    string Category,
    string Message,
    string? Exception = null,
    string? RequestPath = null,
    string? RemoteIp = null,
    int? StatusCode = null,
    long? DurationMs = null);
