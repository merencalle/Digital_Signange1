using System.IO;

namespace DigitalSignage.WindowsPlayer.Services;

public static class DeviceIdentity
{
    private static readonly string IdFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "DigitalSignage",
        "device.id");

    public static string GetOrCreateUniqueId()
    {
        if (File.Exists(IdFilePath))
        {
            var existing = File.ReadAllText(IdFilePath).Trim();
            if (!string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }
        }

        var uniqueId = Guid.NewGuid().ToString();
        Directory.CreateDirectory(Path.GetDirectoryName(IdFilePath)!);
        File.WriteAllText(IdFilePath, uniqueId);
        return uniqueId;
    }
}
