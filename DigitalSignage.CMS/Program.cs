using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.CMS.Endpoints;
using DigitalSignage.CMS.Logging;
using DigitalSignage.CMS.Services;
using Xabe.FFmpeg;

const long MaxUploadBytes = 1024L * 1024 * 1024; // 1 GiB

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<MediaConversionService>();

builder.Services.AddSingleton<LogEntryQueue>();
builder.Services.AddSingleton<Microsoft.Extensions.Logging.ILoggerProvider, DatabaseLoggerProvider>();
builder.Services.AddHostedService<LogFlushService>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = MaxUploadBytes;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = MaxUploadBytes;
});

var app = builder.Build();

var ffmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg-bin");
await FFmpegProvisioner.EnsureInstalledAsync(ffmpegPath);
FFmpeg.SetExecutablesPath(ffmpegPath);

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
app.UseAntiforgery();

app.MapRazorPages();
app.MapDeviceEndpoints();

app.Run();