using System.Diagnostics;

namespace DigitalSignage.CMS.Logging;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly LogEntryQueue _queue;

    public RequestLoggingMiddleware(RequestDelegate next, LogEntryQueue queue)
    {
        _next = next;
        _queue = queue;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var path = context.Request.Path.Value ?? string.Empty;
            if (!IsStaticAsset(path))
            {
                _queue.Enqueue(new LogEntryMessage(
                    DateTime.UtcNow,
                    "Information",
                    "Traffic",
                    $"{context.Request.Method} {path} -> {context.Response.StatusCode}",
                    RequestPath: path,
                    RemoteIp: context.Connection.RemoteIpAddress?.ToString(),
                    StatusCode: context.Response.StatusCode,
                    DurationMs: stopwatch.ElapsedMilliseconds));
            }
        }
    }

    private static bool IsStaticAsset(string path) =>
        path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/media/", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase);
}
