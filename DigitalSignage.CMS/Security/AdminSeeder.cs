using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace DigitalSignage.CMS.Security;

public static class AdminSeeder
{
    public const string DefaultUserName = "admin";
    public const string DefaultPassword = "ChangeMe123!";

    public static async Task EnsureAdminUserAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();

        if (userManager.Users.Any())
        {
            return;
        }

        var admin = new IdentityUser
        {
            UserName = DefaultUserName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, DefaultPassword);
        if (result.Succeeded)
        {
            logger.LogWarning(
                "No admin account existed, so one was created automatically. Username: '{Username}', Password: '{Password}'. Change this password after logging in.",
                DefaultUserName, DefaultPassword);
        }
        else
        {
            logger.LogError(
                "Failed to seed default admin account: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
