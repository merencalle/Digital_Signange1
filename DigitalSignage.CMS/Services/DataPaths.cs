namespace DigitalSignage.CMS.Services;

/// <summary>Absolute, already-resolved paths to the app's persisted data, shared by services that need them.</summary>
public class DataPaths
{
    public required string DatabasePath { get; init; }
    public required string MediaPath { get; init; }
    public required string BackupRoot { get; init; }
}
