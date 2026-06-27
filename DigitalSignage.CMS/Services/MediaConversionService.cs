using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using Xabe.FFmpeg;

namespace DigitalSignage.CMS.Services;

public class MediaConversionService
{
    public static async Task<string> ComputeFileHashAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static readonly string[] NativeImageExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".tiff", ".tif" };
    private static readonly string[] ConvertibleImageExtensions = { ".webp", ".heic", ".heif" };
    private static readonly string[] NativeVideoExtensions = { ".mp4" };
    private static readonly string[] ConvertibleVideoExtensions = { ".mov", ".avi", ".mkv", ".wmv", ".webm", ".flv" };

    public static bool IsSupportedImage(string extension) =>
        NativeImageExtensions.Contains(extension) || ConvertibleImageExtensions.Contains(extension);

    public static bool IsSupportedVideo(string extension) =>
        NativeVideoExtensions.Contains(extension) || ConvertibleVideoExtensions.Contains(extension);

    public async Task<string> ProcessImageAsync(string tempInputPath, string extension, string destinationDirectory)
    {
        extension = extension.ToLowerInvariant();

        if (NativeImageExtensions.Contains(extension))
        {
            var fileName = $"{Guid.NewGuid()}{extension}";
            File.Copy(tempInputPath, Path.Combine(destinationDirectory, fileName));
            return fileName;
        }

        var pngFileName = $"{Guid.NewGuid()}.png";
        using var image = await Image.LoadAsync(tempInputPath);
        await image.SaveAsync(Path.Combine(destinationDirectory, pngFileName), new PngEncoder());
        return pngFileName;
    }

    public async Task<string> ProcessVideoAsync(string tempInputPath, string extension, string destinationDirectory)
    {
        extension = extension.ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}.mp4";
        var destinationPath = Path.Combine(destinationDirectory, fileName);

        if (NativeVideoExtensions.Contains(extension))
        {
            File.Copy(tempInputPath, destinationPath);
            return fileName;
        }

        var mediaInfo = await FFmpeg.GetMediaInfo(tempInputPath);
        var videoStream = mediaInfo.VideoStreams.FirstOrDefault()?.SetCodec(VideoCodec.h264);
        var audioStream = mediaInfo.AudioStreams.FirstOrDefault()?.SetCodec(AudioCodec.aac);

        var conversion = FFmpeg.Conversions.New().SetOutput(destinationPath);
        if (videoStream is not null)
        {
            conversion.AddStream(videoStream);
        }
        if (audioStream is not null)
        {
            conversion.AddStream(audioStream);
        }

        await conversion.Start();
        return fileName;
    }
}
