using StangaNetLib.ContentFlow.Auditing;
using StangaNetLib.ContentFlow.Content;
using StangaNetLib.ContentFlow.Exceptions;
using StangaNetLib.Core.Common;
using StangaNetLib.Core.Guards;

namespace StangaNetLib.ContentFlow.Workflow;

/// <summary>
/// Default implementation of <see cref="IContentWorkflowService{T}"/>.
/// Enforces the state machine transition rules, persists changes, and appends audit log entries.
/// </summary>
internal sealed class ContentWorkflowService<T>(
    IContentRepository<T> repository,
    IContentReviewLogRepository reviewLog,
    TimeProvider timeProvider) : IContentWorkflowService<T>
{
    private readonly IContentRepository<T> _repository = Guard.Against.Null(repository, nameof(repository));
    private readonly IContentReviewLogRepository _reviewLog = Guard.Against.Null(reviewLog, nameof(reviewLog));
    private readonly TimeProvider _timeProvider = Guard.Against.Null(timeProvider, nameof(timeProvider));

    public async Task<Result<ContentItem<T>>> CreateAsync(T payload, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(payload, nameof(payload));

        var now = _timeProvider.GetUtcNow();
        var item = new ContentItem<T>
        {
            Id        = Guid.NewGuid(),
            State     = ContentState.Draft,
            Payload   = payload,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var saved = await _repository.AddAsync(item, ct);
        return Result<ContentItem<T>>.Success(saved);
    }

    public Task<Result<ContentItem<T>>> SubmitForReviewAsync(
        Guid contentId, string actor, string? note = null, CancellationToken ct = default)
        => TransitionAsync(contentId, ContentState.Pending, actor, note, ct);

    public Task<Result<ContentItem<T>>> ApproveAsync(
        Guid contentId, string actor, string? note = null, CancellationToken ct = default)
        => TransitionAsync(contentId, ContentState.Approved, actor, note, ct);

    public Task<Result<ContentItem<T>>> RequestRevisionAsync(
        Guid contentId, string actor, string note, CancellationToken ct = default)
    {
        Guard.Against.NullOrWhiteSpace(note, nameof(note));
        return TransitionAsync(contentId, ContentState.Draft, actor, note, ct);
    }

    public async Task<Result<ContentItem<T>>> PublishAsync(
        Guid contentId,
        string actor,
        DateTimeOffset? expiresAt = null,
        string? note = null,
        CancellationToken ct = default)
    {
        Guard.Against.EmptyGuid(contentId, nameof(contentId));
        Guard.Against.NullOrWhiteSpace(actor, nameof(actor));

        var existing = await _repository.FindByIdAsync(contentId, ct);
        if (existing is null)
            return Result<ContentItem<T>>.Failure(ContentFlowErrors.ContentNotFound(contentId));

        if (!ContentTransitionRules.IsAllowed(existing.State, ContentState.Published))
            return Result<ContentItem<T>>.Failure(ContentFlowErrors.InvalidTransition(existing.State, ContentState.Published));

        var now = _timeProvider.GetUtcNow();

        if (expiresAt.HasValue && expiresAt.Value <= now)
            return Result<ContentItem<T>>.Failure(ContentFlowErrors.ExpiryInThePast(expiresAt.Value));

        var updated = existing with
        {
            State              = ContentState.Published,
            UpdatedAt          = now,
            ScheduledPublishAt = null,
            ExpiresAt          = expiresAt,
        };

        var saved = await _repository.UpdateAsync(updated, ct);
        await _reviewLog.AddAsync(BuildLog(contentId, existing.State, ContentState.Published, actor, note, now), ct);

        return Result<ContentItem<T>>.Success(saved);
    }

    public async Task<Result<ContentItem<T>>> SchedulePublishAsync(
        Guid contentId,
        DateTimeOffset publishAt,
        string actor,
        string? note = null,
        CancellationToken ct = default)
    {
        Guard.Against.EmptyGuid(contentId, nameof(contentId));
        Guard.Against.NullOrWhiteSpace(actor, nameof(actor));

        var existing = await _repository.FindByIdAsync(contentId, ct);
        if (existing is null)
            return Result<ContentItem<T>>.Failure(ContentFlowErrors.ContentNotFound(contentId));

        if (existing.State != ContentState.Approved)
            return Result<ContentItem<T>>.Failure(ContentFlowErrors.SchedulePublishRequiresApproved(existing.State));

        var now = _timeProvider.GetUtcNow();
        if (publishAt <= now)
            return Result<ContentItem<T>>.Failure(ContentFlowErrors.ScheduledTimeInThePast(publishAt));

        var updated = existing with
        {
            ScheduledPublishAt = publishAt,
            UpdatedAt          = now,
        };

        var saved = await _repository.UpdateAsync(updated, ct);

        var scheduleNote = string.IsNullOrWhiteSpace(note)
            ? $"Scheduled for publish at {publishAt:O}."
            : $"Scheduled for publish at {publishAt:O}. {note}";

        await _reviewLog.AddAsync(BuildLog(contentId, ContentState.Approved, ContentState.Approved, actor, scheduleNote, now), ct);

        return Result<ContentItem<T>>.Success(saved);
    }

    public Task<Result<ContentItem<T>>> RevokeAsync(
        Guid contentId, string actor, string reason, CancellationToken ct = default)
    {
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
        return TransitionAsync(contentId, ContentState.Revoked, actor, reason, ct);
    }

    public async Task<Result<ContentItem<T>>> GetAsync(Guid contentId, CancellationToken ct = default)
    {
        Guard.Against.EmptyGuid(contentId, nameof(contentId));

        var item = await _repository.FindByIdAsync(contentId, ct);
        if (item is null)
            return Result<ContentItem<T>>.Failure(ContentFlowErrors.ContentNotFound(contentId));

        return Result<ContentItem<T>>.Success(item);
    }

    public async Task<Result<IReadOnlyList<ContentItem<T>>>> GetByStateAsync(ContentState state, CancellationToken ct = default)
    {
        Guard.Against.InvalidEnum(state, nameof(state));
        var items = await _repository.FindByStateAsync(state, ct);
        return Result<IReadOnlyList<ContentItem<T>>>.Success(items);
    }

    private async Task<Result<ContentItem<T>>> TransitionAsync(
        Guid contentId,
        ContentState to,
        string actor,
        string? note,
        CancellationToken ct)
    {
        Guard.Against.EmptyGuid(contentId, nameof(contentId));
        Guard.Against.NullOrWhiteSpace(actor, nameof(actor));

        var existing = await _repository.FindByIdAsync(contentId, ct);
        if (existing is null)
            return Result<ContentItem<T>>.Failure(ContentFlowErrors.ContentNotFound(contentId));

        if (!ContentTransitionRules.IsAllowed(existing.State, to))
            return Result<ContentItem<T>>.Failure(ContentFlowErrors.InvalidTransition(existing.State, to));

        var now = _timeProvider.GetUtcNow();
        var updated = existing with
        {
            State              = to,
            UpdatedAt          = now,
            ScheduledPublishAt = to == ContentState.Draft ? null : existing.ScheduledPublishAt,
        };

        var saved = await _repository.UpdateAsync(updated, ct);
        await _reviewLog.AddAsync(BuildLog(contentId, existing.State, to, actor, note, now), ct);

        return Result<ContentItem<T>>.Success(saved);
    }

    private static ContentReviewLog BuildLog(
        Guid contentId,
        ContentState from,
        ContentState to,
        string actor,
        string? note,
        DateTimeOffset at) => new()
    {
        Id         = Guid.NewGuid(),
        ContentId  = contentId,
        FromState  = from,
        ToState    = to,
        Actor      = actor,
        Note       = note,
        OccurredAt = at,
    };
}
