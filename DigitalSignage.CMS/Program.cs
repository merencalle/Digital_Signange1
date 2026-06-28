using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.CMS.Endpoints;
using DigitalSignage.CMS.Logging;
using DigitalSignage.CMS.Security;
using DigitalSignage.CMS.Services;
using Xabe.FFmpeg;

const long MaxUploadBytes = 1024L * 1024 * 1024; // 1 GiB

// ContentRootPath defaults to the process's current working directory, not the
// app's own folder. That's harmless under `dotnet run` (CWD = project folder) but
// breaks every path derived from it (the SQLite file, App_Data/certs, wwwroot) the
// moment this runs any other way - notably as a Windows Service, whose CWD
// defaults to C:\Windows\System32. Anchor it explicitly to where the binary lives.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// No-op unless actually launched by the Windows Service Control Manager -
// safe to always include. Lets this run as a real Windows Service (auto-start,
// survives reboots, no logged-in session required) on a deployment server.
builder.Host.UseWindowsService();

// Add services to the container.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Roles.SuperAdminPolicy, p => p.RequireRole(Roles.SuperAdmin));
    options.AddPolicy(Roles.OperatorsPolicy, p => p.RequireRole(Roles.SuperAdmin, Roles.Admin));
    options.AddPolicy(Roles.ContentTeamPolicy, p => p.RequireRole(Roles.SuperAdmin, Roles.Admin, Roles.Manager));
    options.AddPolicy(Roles.ManagerPolicy, p => p.RequireRole(Roles.SuperAdmin, Roles.Manager));
});

builder.Services.AddRazorPages(options =>
{
    // Default: any authenticated user (role-specific tightening happens per folder below).
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");

    options.Conventions.AuthorizeFolder("/Devices", Roles.OperatorsPolicy);
    options.Conventions.AuthorizeFolder("/Logs", Roles.OperatorsPolicy);
    options.Conventions.AuthorizeFolder("/Users", Roles.SuperAdminPolicy);
    options.Conventions.AuthorizePage("/Wizard/Index", Roles.SuperAdminPolicy);
    options.Conventions.AuthorizePage("/Wizard/Certificate", Roles.SuperAdminPolicy);
    options.Conventions.AuthorizePage("/Wizard/Enrollment", Roles.OperatorsPolicy);
    options.Conventions.AuthorizeFolder("/Playlists", Roles.ContentTeamPolicy);
    options.Conventions.AuthorizeFolder("/Approvals", Roles.ManagerPolicy);
});

// Resolve the SQLite file against ContentRootPath rather than trusting the process's
// current working directory - that's the project folder when run via `dotnet run`,
// but defaults to C:\Windows\System32 for a launched-by-SCM Windows Service.
var sqliteConnectionString = ResolveSqliteConnectionString(
    builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=digitalsignage.db",
    builder.Environment.ContentRootPath);

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(sqliteConnectionString));

builder.Services.AddSingleton(new DataPaths
{
    DatabasePath = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(sqliteConnectionString).DataSource,
    MediaPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "media"),
    BackupRoot = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "backups"),
});
builder.Services.AddHostedService<BackupService>();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
});

builder.Services.AddScoped<MediaConversionService>();
builder.Services.AddSingleton<CertificateService>();
builder.Services.AddSingleton<PlayerPackager>();

builder.Services.AddSingleton<LogEntryQueue>();
builder.Services.AddSingleton<Microsoft.Extensions.Logging.ILoggerProvider, DatabaseLoggerProvider>();
builder.Services.AddHostedService<LogFlushService>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = MaxUploadBytes;
});

builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 5110;
});

var managedCertPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "certs", "cms.pfx");

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = MaxUploadBytes;
    // HTTPS is the channel devices/players should use - all traffic between
    // Sentinel and the player (registration, heartbeat, playlist, media) is TLS-encrypted.
    options.ListenAnyIP(5109);
    options.ListenAnyIP(5110, listenOptions =>
    {
        if (File.Exists(managedCertPath))
        {
            // Wizard-generated/uploaded certificate (self-signed, CA-signed, or AD CS-issued).
            listenOptions.UseHttps(managedCertPath);
        }
        else
        {
            // No managed cert yet - fall back to the ASP.NET Core dev certificate.
            listenOptions.UseHttps();
        }
    });
});

var app = builder.Build();

var ffmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg-bin");
await FFmpegProvisioner.EnsureInstalledAsync(ffmpegPath);
FFmpeg.SetExecutablesPath(ffmpegPath);

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

await AdminSeeder.EnsureAdminUserAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.Logger.LogWarning(
        "Running in Development mode - detailed exception pages are enabled and HSTS is off. " +
        "Do not leave a real deployment set this way; this should only happen on a dev machine.");
}

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();

// A temporary (admin-set or freshly-seeded) password forces a change before anything else works.
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isExempt = path.StartsWithSegments("/Account/ChangePassword") ||
        path.StartsWithSegments("/Account/Logout") ||
        path.StartsWithSegments("/lib") ||
        path.StartsWithSegments("/css") ||
        path.StartsWithSegments("/js");

    if (!isExempt && context.User.HasClaim(c => c.Type == SentinelClaims.MustChangePassword))
    {
        context.Response.Redirect("/Account/ChangePassword");
        return;
    }

    await next();
});

app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorPages();
app.MapDeviceEndpoints();

app.Run();

static string ResolveSqliteConnectionString(string connectionString, string contentRoot)
{
    var csb = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
    if (!Path.IsPathRooted(csb.DataSource))
    {
        csb.DataSource = Path.Combine(contentRoot, csb.DataSource);
    }
    return csb.ToString();
}