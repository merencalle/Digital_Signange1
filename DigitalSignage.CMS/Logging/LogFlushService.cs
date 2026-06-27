using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.CMS.Models;

namespace DigitalSignage.CMS.Logging;

public class LogFlushService : BackgroundService
{
    private const int MaxRetainedEntries = 10_000;
    private const int MaxBatchSize = 500;

    private readonly LogEntryQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;

    public LogFlushService(LogEntryQueue queue, IServiceScopeFactory scopeFactory)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                await FlushAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        await FlushAsync();
    }

    private async Task FlushAsync()
    {
        var batch = new List<LogEntry>();
        while (batch.Count < MaxBatchSize && _queue.TryDequeue(out var message))
        {
            batch.Add(new LogEntry
            {
                Timestamp = message!.Timestamp,
                Level = message.Level,
                Category = message.Category,
                Message = message.Message,
                Exception = message.Exception,
                RequestPath = message.RequestPath,
                RemoteIp = message.RemoteIp,
                StatusCode = message.StatusCode,
                DurationMs = message.DurationMs
            });
        }

        if (batch.Count == 0)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.LogEntries.AddRange(batch);
        await db.SaveChangesAsync();

        var totalCount = await db.LogEntries.CountAsync();
        if (totalCount > MaxRetainedEntries)
        {
            var staleEntries = await db.LogEntries
                .OrderBy(l => l.Id)
                .Take(totalCount - MaxRetainedEntries)
                .ToListAsync();
            db.LogEntries.RemoveRange(staleEntries);
            await db.SaveChangesAsync();
        }
    }
}
