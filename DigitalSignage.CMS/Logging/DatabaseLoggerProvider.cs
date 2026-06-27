using Microsoft.Extensions.Logging;

namespace DigitalSignage.CMS.Logging;

public class DatabaseLoggerProvider : ILoggerProvider
{
    private readonly LogEntryQueue _queue;

    public DatabaseLoggerProvider(LogEntryQueue queue)
    {
        _queue = queue;
    }

    public ILogger CreateLogger(string categoryName) => new DatabaseLogger(categoryName, _queue);

    public void Dispose()
    {
    }

    private class DatabaseLogger : ILogger
    {
        private readonly string _category;
        private readonly LogEntryQueue _queue;

        public DatabaseLogger(string category, LogEntryQueue queue)
        {
            _category = category;
            _queue = queue;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            // Hard exclude EF Core's own command/connection logging: capturing it would
            // mean every DB write-to-log triggers more DB activity to log, ad infinitum.
            if (_category.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
            {
                return;
            }

            _queue.Enqueue(new LogEntryMessage(
                DateTime.UtcNow,
                logLevel.ToString(),
                _category,
                formatter(state, exception),
                exception?.ToString()));
        }
    }
}
