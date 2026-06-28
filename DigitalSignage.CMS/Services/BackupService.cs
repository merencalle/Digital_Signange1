using Microsoft.Data.Sqlite;

namespace DigitalSignage.CMS.Services;

/// <summary>
/// Periodically backs up the SQLite database and the uploaded media folder.
/// No automated backup existed before this - both the database and every
/// uploaded content file lived in exactly one place with no recovery path.
/// </summary>
public class BackupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private const int RetainCount = 7;

    private readonly DataPaths _paths;
    private readonly ILogger<BackupService> _logger;

    public BackupService(DataPaths paths, ILogger<BackupService> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Don't compete with startup work (migration, FFmpeg provisioning, etc.).
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                RunBackup();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled backup failed.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void RunBackup()
    {
        Directory.CreateDirectory(_paths.BackupRoot);

        var destination = Path.Combine(_paths.BackupRoot, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(destination);

        BackupDatabase(destination);
        BackupMedia(destination);
        PruneOldBackups();

        _logger.LogInformation("Backup completed: {Destination}", destination);
    }

    private void BackupDatabase(string destination)
    {
        if (!File.Exists(_paths.DatabasePath))
        {
            return;
        }

        var destinationDbPath = Path.Combine(destination, Path.GetFileName(_paths.DatabasePath));

        // SQLite's online backup API, not a plain file copy - safe to run against a database
        // that's actively being written to, unlike copying the file directly.
        using var source = new SqliteConnection($"Data Source={_paths.DatabasePath};Mode=ReadOnly");
        using var dest = new SqliteConnection($"Data Source={destinationDbPath}");
        source.Open();
        dest.Open();
        source.BackupDatabase(dest);
    }

    private void BackupMedia(string destination)
    {
        if (!Directory.Exists(_paths.MediaPath))
        {
            return;
        }

        var destinationMediaPath = Path.Combine(destination, "media");
        Directory.CreateDirectory(destinationMediaPath);

        foreach (var file in Directory.EnumerateFiles(_paths.MediaPath))
        {
            File.Copy(file, Path.Combine(destinationMediaPath, Path.GetFileName(file)), overwrite: true);
        }
    }

    private void PruneOldBackups()
    {
        var backups = Directory.GetDirectories(_paths.BackupRoot)
            .OrderByDescending(d => d)
            .Skip(RetainCount);

        foreach (var stale in backups)
        {
            Directory.Delete(stale, recursive: true);
        }
    }
}
