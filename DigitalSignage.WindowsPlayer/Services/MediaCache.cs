using System.IO;
using System.Text.Json;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.WindowsPlayer.Services;

public class MediaCache
{
    private readonly string _cacheDirectory;
    private readonly string _manifestPath;
    private readonly Dictionary<int, CacheEntry> _manifest;

    public MediaCache()
    {
        _cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "DigitalSignage",
            "cache");
        Directory.CreateDirectory(_cacheDirectory);

        _manifestPath = Path.Combine(_cacheDirectory, "manifest.json");
        _manifest = LoadManifest();
    }

    public async Task<string> EnsureCachedAsync(ContentItem item, Func<string, Task> downloadToAsync)
    {
        if (_manifest.TryGetValue(item.Id, out var entry) &&
            entry.Hash == item.FileHash &&
            File.Exists(entry.LocalPath))
        {
            return entry.LocalPath;
        }

        var extension = Path.GetExtension(item.FilePath);
        var localPath = Path.Combine(_cacheDirectory, $"{item.Id}{extension}");
        var partialPath = localPath + ".partial";

        await downloadToAsync(partialPath);

        if (File.Exists(localPath))
        {
            File.Delete(localPath);
        }
        File.Move(partialPath, localPath);

        _manifest[item.Id] = new CacheEntry { Hash = item.FileHash, LocalPath = localPath };
        SaveManifest();

        return localPath;
    }

    private Dictionary<int, CacheEntry> LoadManifest()
    {
        if (!File.Exists(_manifestPath))
        {
            return new();
        }

        try
        {
            var json = File.ReadAllText(_manifestPath);
            return JsonSerializer.Deserialize<Dictionary<int, CacheEntry>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private void SaveManifest()
    {
        var json = JsonSerializer.Serialize(_manifest);
        File.WriteAllText(_manifestPath, json);
    }

    private class CacheEntry
    {
        public string Hash { get; set; } = string.Empty;
        public string LocalPath { get; set; } = string.Empty;
    }
}
