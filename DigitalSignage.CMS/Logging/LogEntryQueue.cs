using System.Collections.Concurrent;

namespace DigitalSignage.CMS.Logging;

public class LogEntryQueue
{
    private readonly ConcurrentQueue<LogEntryMessage> _queue = new();

    public void Enqueue(LogEntryMessage message) => _queue.Enqueue(message);

    public bool TryDequeue(out LogEntryMessage? message) => _queue.TryDequeue(out message);
}
