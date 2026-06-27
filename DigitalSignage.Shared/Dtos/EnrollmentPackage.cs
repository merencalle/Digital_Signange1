namespace DigitalSignage.Shared.Dtos;

public class EnrollmentPackage
{
    public string CmsBaseUrl { get; set; } = string.Empty;
    public string PairingSecret { get; set; } = string.Empty;
    public string? PinnedCertThumbprint { get; set; }
}
