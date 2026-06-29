namespace DigitalSignage.CMS.Services;

/// <summary>
/// Absolute, already-resolved paths to the app's persisted (writable) data.
///
/// Deliberately separate from the install directory (ContentRootPath/AppContext.BaseDirectory):
/// when running as a Windows Service installed under Program Files, that folder is
/// NTFS-protected (and often blocked by Defender's Controlled Folder Access too) -
/// writing the database there fails outright. Interactively (dotnet run, dev),
/// DataRoot is just the project's own folder, same as it's always been.
/// </summary>
public class DataPaths
{
    public required string DataRoot { get; init; }
    public required string DatabasePath { get; init; }
    public required string MediaPath { get; init; }
    public required string BackupRoot { get; init; }
    public required string CertDirectory { get; init; }
}
