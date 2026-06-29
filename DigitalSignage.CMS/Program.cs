using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting.WindowsServices;
using DigitalSignage.CMS.Data;
using DigitalSignage.CMS.Endpoints;
using DigitalSignage.CMS.Logging;
using DigitalSignage.CMS.Security;
using DigitalSignage.CMS.Services;
using Xabe.FFmpeg;

const long MaxUploadBytes = 1024L * 1024 * 1024; // 1 GiB

// ContentRootPath defaults to the process's current working directory, not the
// app's own folder. That's harmless under `dotnet run` (CWD = project folder) but
// breaks every path derived from it the moment this runs any other way - notably
// as a Windows Service, whose CWD defaults to C:\Windows\System32. Anchor it
// explicitly to where the binary lives.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// No-op unless actually launched by the Windows Service Control Manager -
// safe to always include. Lets this run as a real Windows Service (auto-start,
// survives reboots, no logged-in session required) on a deployment server.
builder.Host.UseWindowsService();

// Writable data (database, certs, backups, uploaded media) must NOT live under the
// install directory when running as a service: Program Files is NTFS-protected
// (and often blocked outright by Defender's Controlled Folder Access too) - writing
// the database there fails with "unable to open database file". Use a dedicated
// ProgramData folder for that case; interactively (dotnet run) keep using the
// project's own folder, exactly as before.
var dataRoot = WindowsServiceHelpers.IsWindowsService()
    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Sentinel")
    : builder.Environment.ContentRootPath;
Directory.CreateDirectory(dataRoot);

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

var sqliteConnectionString = ResolveSqliteConnectionString(
    builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=digitalsignage.db",
    dataRoot);

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(sqliteConnectionString));

var dataPaths = new DataPaths
{
    DataRoot = dataRoot,
    DatabasePath = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(sqliteConnectionString).DataSource,
    MediaPath = Path.Combine(dataRoot, "media"),
    BackupRoot = Path.Combine(dataRoot, "backups"),
    CertDirectory = Path.Combine(dataRoot, "certs"),
};
Directory.CreateDirectory(dataPaths.MediaPath);
builder.Services.AddSingleton(dataPaths);
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

// Constructed (and, if needed, seeded with a bootstrap certificate) directly here rather
// than left to plain DI registration, because Kestrel needs a certificate to bind to
// before the request pipeline - and therefore before any DI-resolved service - exists.
var certService = new CertificateService(dataPaths);
if (certService.GetCurrentCertificateInfo() is null)
{
    // No managed cert yet (fresh install). Falling back to the ASP.NET Core dev
    // certificate here would be wrong as a Windows Service: that certificate lives
    // in the *interactive user's* certificate store, not the service account's
    // (typically LocalSystem) - so Kestrel would find nothing and crash at startup.
    // Generate a real bootstrap certificate instead; the Deployment Wizard can
    // replace it with a proper one (self-signed/CA-issued) once you're logged in.
    certService.GenerateSelfSigned(Environment.MachineName, validityYears: 5);
}
builder.Services.AddSingleton(certService);
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

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = MaxUploadBytes;
    // HTTPS is the channel devices/players should use - all traffic between
    // Sentinel and the player (registration, heartbeat, playlist, media) is TLS-encrypted.
    // certService guarantees cms.pfx exists by this point (bootstrap-generated above
    // if the Wizard hasn't produced a real one yet).
    options.ListenAnyIP(5109);
    options.ListenAnyIP(5110, listenOptions => listenOptions.UseHttps(certService.PfxPath));
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

// Uploaded content lives in DataPaths.MediaPath, not under wwwroot - see the
// dataRoot comment above. Map it to the same /media URL space the rest of the
// app (and every Player) already expects.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(dataPaths.MediaPath),
    RequestPath = "/media",
});

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