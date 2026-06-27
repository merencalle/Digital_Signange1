using System.IO;
using System.Text.Json;
using DigitalSignage.Shared.Dtos;

namespace DigitalSignage.WindowsPlayer.Services;

public static class EnrollmentLoader
{
    private const string FileName = "enrollment.json";
    private static string PackagePath => Path.Combine(AppContext.BaseDirectory, FileName);
    private static string ConsumedPackagePath => Path.Combine(AppContext.BaseDirectory, FileName + ".used");

    public static EnrollmentPackage? TryLoad()
    {
        if (!File.Exists(PackagePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(PackagePath);
            return JsonSerializer.Deserialize<EnrollmentPackage>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Renames the package after a successful first registration so it's obvious it's been used.</summary>
    public static void MarkConsumed()
    {
        if (File.Exists(PackagePath))
        {
            File.Move(PackagePath, ConsumedPackagePath, overwrite: true);
        }
    }
}
