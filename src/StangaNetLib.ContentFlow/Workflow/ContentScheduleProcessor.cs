using StangaNetLib.ContentFlow.Auditing;
using StangaNetLib.ContentFlow.Content;

namespace StangaNetLib.ContentFlow.Workflow;

/// <summary>
/// Processes scheduled publishes and expiry revocations for a single content type <typeparamref name="T"/>.
/// </summary>
internal sealed class ContentScheduleProcessor<T> : IContentScheduleProcessor
{
    private readonly IContentRepository<T> _repository;
    private readonly IContentReviewLogRepository _reviewLog;
    private readonly TimeProvider _timeProvider;
    private readonly string _actorName;

    internal ContentScheduleProcessor(
        IContentRepository<T> repository,
        IContentReviewLogRepository reviewLog,
        TimeProvider timeProvider,
        string actorName)
    {
        _repository  = repository;
        _reviewLog   = reviewLog;
        _timeProvider = timeProvider;
        _actorName   = actorName;
    }

    public async Task ProcessDueAsync(CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow();

        await PublishScheduledAsync(now, ct);
        await RevokeExpiredAsync(now, ct);
    }

    private async Task PublishScheduledAsync(DateTimeOffset now, CancellationToken ct)
    {
        IReadOnlyList<ContentItem<T>> due;
        try { due = await _repository.FindScheduledForPublishAsync(now, ct); }
        catch (OperationCanceledException) { throw; }
        catch { return; }

        foreach (var item in due)
        {
            try
            {
                var updated = item with
                {
                    State              = ContentState.Published,
                    ScheduledPublishAt = null,
                    UpdatedAt          = now,
                };
                await _repository.UpdateAsync(updated, ct);
                await _reviewLog.AddAsync(new ContentReviewLog
                {
                    Id        = Guid.NewGuid(),
                    ContentId = item.Id,
                    FromState = ContentState.Approved,
                    ToState   = ContentState.Published,
                    Actor     = _actorName,
                    Note      = "Automatically published by scheduler.",
                    OccurredAt = now,
                }, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch { /* one bad item must not block the rest */ }
        }
    }

    private async Task RevokeExpiredAsync(DateTimeOffset now, CancellationToken ct)
    {
        IReadOnlyList<ContentItem<T>> expired;
        try { expired = await _repository.FindExpiredAsync(now, ct); }
        catch (OperationCanceledException) { throw; }
        catch { return; }

        foreach (var item in expired)
        {
            try
            {
                var updated = item with
                {
                    State     = ContentState.Revoked,
                    UpdatedAt = now,
                };
                await _repository.UpdateAsync(updated, ct);
                await _reviewLog.AddAsync(new ContentReviewLog
                {
                    Id        = Guid.NewGuid(),
                    ContentId = item.Id,
                    FromState = ContentState.Published,
                    ToState   = ContentState.Revoked,
                    Actor     = _actorName,
                    Note      = "Automatically revoked by scheduler (content expired).",
                    OccurredAt = now,
                }, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch { /* one bad item must not block the rest */ }
        }
    }
}
