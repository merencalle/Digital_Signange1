using System.IO.Compression;
using System.Text;
using System.Text.Json;
using DigitalSignage.Shared.Dtos;

namespace DigitalSignage.CMS.Services;

public class PlayerPackager
{
    private readonly IWebHostEnvironment _environment;

    public PlayerPackager(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public bool IsPlayerBuildAvailable => FindPlayerBuildDirectory() is not null;

    /// <summary>
    /// Builds a downloadable zip containing the Player executable plus a ready-to-use
    /// enrollment.json, so running the exe is the only step left on the target machine.
    /// </summary>
    public byte[] CreatePackage(EnrollmentPackage enrollment)
    {
        var buildDir = FindPlayerBuildDirectory()
            ?? throw new InvalidOperationException("Player build output was not found. Build DigitalSignage.WindowsPlayer first.");

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var filePath in Directory.GetFiles(buildDir))
            {
                var fileName = Path.GetFileName(filePath);
                if (fileName.StartsWith("enrollment.json", StringComparison.OrdinalIgnoreCase))
                {
                    continue; // never bundle a stale enrollment file left over from local testing
                }

                archive.CreateEntryFromFile(filePath, fileName, CompressionLevel.Optimal);
            }

            var enrollmentEntry = archive.CreateEntry("enrollment.json", CompressionLevel.Optimal);
            using var entryStream = enrollmentEntry.Open();
            var json = JsonSerializer.Serialize(enrollment, new JsonSerializerOptions { WriteIndented = true });
            entryStream.Write(Encoding.UTF8.GetBytes(json));
        }

        return memoryStream.ToArray();
    }

    private string? FindPlayerBuildDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(_environment.ContentRootPath, "..", "DigitalSignage.WindowsPlayer", "bin", "Release", "net8.0-windows"),
            Path.Combine(_environment.ContentRootPath, "..", "DigitalSignage.WindowsPlayer", "bin", "Debug", "net8.0-windows")
        };

        return candidates
            .Select(Path.GetFullPath)
            .FirstOrDefault(dir => File.Exists(Path.Combine(dir, "DigitalSignage.WindowsPlayer.exe")));
    }
}
