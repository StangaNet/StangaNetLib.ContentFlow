using StangaNetLib.ContentFlow.Auditing;

namespace StangaNetLib.ContentFlow.Auditing;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IContentReviewLogRepository"/>.
/// Registered as the default repository when <c>AddStangaNetLibContentFlow</c> is called.
/// </summary>
internal sealed class InMemoryContentReviewLogRepository : IContentReviewLogRepository
{
    private readonly List<ContentReviewLog> _logs = [];
    private readonly object _lock = new();

    public Task<IReadOnlyList<ContentReviewLog>> GetByContentIdAsync(Guid contentId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var result = _logs.Where(l => l.ContentId == contentId).ToList();
            return Task.FromResult<IReadOnlyList<ContentReviewLog>>(result);
        }
    }

    public Task AddAsync(ContentReviewLog entry, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _logs.Add(entry);
        }
        return Task.CompletedTask;
    }
}
