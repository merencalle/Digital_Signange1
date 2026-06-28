namespace DigitalSignage.CMS.Security;

public static class SentinelClaims
{
    /// <summary>Present (value "true") on an account that must change its password before doing anything else.</summary>
    public const string MustChangePassword = "MustChangePassword";
}
