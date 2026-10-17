using StangaNetLib.ContentFlow.Auditing;

namespace StangaNetLib.ContentFlow.Auditing;

/// <summary>
/// Persistence abstraction for the immutable audit trail of content lifecycle transitions.
/// Entries are append-only; the repository never deletes or modifies records.
/// </summary>
public interface IContentReviewLogRepository
{
    /// <summary>Returns all log entries recorded for the content item identified by <paramref name="contentId"/>.</summary>
    /// <param name="contentId">The identifier of the content item whose log entries to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All audit entries for <paramref name="contentId"/>, ordered by insertion time. Never null; may be empty.</returns>
    Task<IReadOnlyList<ContentReviewLog>> GetByContentIdAsync(Guid contentId, CancellationToken ct = default);

    /// <summary>Appends a new log entry to the audit trail.</summary>
    /// <param name="entry">The log entry to append. Must not be null.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(ContentReviewLog entry, CancellationToken ct = default);
}
