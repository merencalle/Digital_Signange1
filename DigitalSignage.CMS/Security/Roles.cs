namespace DigitalSignage.CMS.Security;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string User = "User";

    public static readonly string[] All = { SuperAdmin, Admin, Manager, User };

    /// <summary>SuperAdmin and Admin - day-to-day operation, device/content/playlist management.</summary>
    public const string OperatorsPolicy = "Operators";

    /// <summary>SuperAdmin only - network/certificate deployment decisions.</summary>
    public const string SuperAdminPolicy = "SuperAdminOnly";

    /// <summary>SuperAdmin, Admin, Manager - content design and approval, but not network deployment.</summary>
    public const string ContentTeamPolicy = "ContentTeam";

    /// <summary>Manager only - the approval queue.</summary>
    public const string ManagerPolicy = "ManagerOnly";
}
