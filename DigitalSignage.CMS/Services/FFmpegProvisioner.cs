using System.IO.Compression;

namespace DigitalSignage.CMS.Services;

public static class FFmpegProvisioner
{
    private const string FFmpegUrl = "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-win-64.zip";
    private const string FFprobeUrl = "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffprobe-6.1-win-64.zip";

    public static async Task EnsureInstalledAsync(string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        var ffmpegExe = Path.Combine(destinationDirectory, "ffmpeg.exe");
        var ffprobeExe = Path.Combine(destinationDirectory, "ffprobe.exe");

        if (File.Exists(ffmpegExe) && File.Exists(ffprobeExe))
        {
            return;
        }

        using var http = new HttpClient();

        if (!File.Exists(ffmpegExe))
        {
            await DownloadAndExtractAsync(http, FFmpegUrl, destinationDirectory);
        }

        if (!File.Exists(ffprobeExe))
        {
            await DownloadAndExtractAsync(http, FFprobeUrl, destinationDirectory);
        }
    }

    private static async Task DownloadAndExtractAsync(HttpClient http, string url, string destinationDirectory)
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        await using (var fileStream = File.Create(zipPath))
        await using (var responseStream = await http.GetStreamAsync(url))
        {
            await responseStream.CopyToAsync(fileStream);
        }

        ZipFile.ExtractToDirectory(zipPath, destinationDirectory, overwriteFiles: true);
        File.Delete(zipPath);
    }
}
