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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    options.ListenAnyIP(5109);
    options.ListenAnyIP(5110, listenOptions => listenOptions.UseHttps());
});

var app = builder.Build();

var ffmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg-bin");
await FFmpegProvisioner.EnsureInstalledAsync(ffmpegPath);
FFmpeg.SetExecutablesPath(ffmpegPath);

await AdminSeeder.EnsureAdminUserAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorPages();
app.MapDeviceEndpoints();

app.Run();