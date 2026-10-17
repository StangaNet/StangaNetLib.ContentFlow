using StangaNetLib.ContentFlow.Content;
using StangaNetLib.Core.Common;

namespace StangaNetLib.ContentFlow.Workflow;

/// <summary>
/// Orchestrates lifecycle transitions for content items of type <typeparamref name="T"/>.
/// All mutating operations produce a new immutable <see cref="ContentItem{T}"/> and append
/// an entry to the audit trail.
/// </summary>
/// <typeparam name="T">The payload type of the managed content items.</typeparam>
public interface IContentWorkflowService<T>
{
    /// <summary>
    /// Creates a new content item in the <see cref="ContentState.Draft"/> state.
    /// </summary>
    /// <param name="payload">The content data. Must not be null.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result{T}"/> containing the created <see cref="ContentItem{T}"/> on success.
    /// </returns>
    Task<Result<ContentItem<T>>> CreateAsync(T payload, CancellationToken ct = default);

    /// <summary>
    /// Transitions a <see cref="ContentState.Draft"/> item to <see cref="ContentState.Pending"/>.
    /// </summary>
    /// <param name="contentId">The identifier of the content item to transition.</param>
    /// <param name="actor">Identity of the user or service performing the action.</param>
    /// <param name="note">Optional note to record in the audit trail.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result{T}"/> containing the updated <see cref="ContentItem{T}"/> on success,
    /// or an error with code <c>Content.NotFound</c> or <c>Content.InvalidTransition</c> on failure.
    /// </returns>
    Task<Result<ContentItem<T>>> SubmitForReviewAsync(Guid contentId, string actor, string? note = null, CancellationToken ct = default);

    /// <summary>
    /// Transitions a <see cref="ContentState.Pending"/> item to <see cref="ContentState.Approved"/>.
    /// </summary>
    /// <param name="contentId">The identifier of the content item to transition.</param>
    /// <param name="actor">Identity of the user or service performing the action.</param>
    /// <param name="note">Optional note to record in the audit trail.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result{T}"/> containing the updated <see cref="ContentItem{T}"/> on success,
    /// or an error with code <c>Content.NotFound</c> or <c>Content.InvalidTransition</c> on failure.
    /// </returns>
    Task<Result<ContentItem<T>>> ApproveAsync(Guid contentId, string actor, string? note = null, CancellationToken ct = default);

    /// <summary>
    /// Transitions a <see cref="ContentState.Pending"/> or <see cref="ContentState.Approved"/> item
    /// back to <see cref="ContentState.Draft"/>, resetting any scheduled publish.
    /// </summary>
    /// <param name="contentId">The identifier of the content item to transition.</param>
    /// <param name="actor">Identity of the user or service performing the action.</param>
    /// <param name="note">Required explanation for the revision request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result{T}"/> containing the updated <see cref="ContentItem{T}"/> on success,
    /// or an error with code <c>Content.NotFound</c> or <c>Content.InvalidTransition</c> on failure.
    /// </returns>
    Task<Result<ContentItem<T>>> RequestRevisionAsync(Guid contentId, string actor, string note, CancellationToken ct = default);

    /// <summary>
    /// Transitions an <see cref="ContentState.Approved"/> item to <see cref="ContentState.Published"/>.
    /// Clears any pending <c>ScheduledPublishAt</c>.
    /// </summary>
    /// <param name="contentId">The identifier of the content item to publish.</param>
    /// <param name="actor">Identity of the user or service performing the action.</param>
    /// <param name="expiresAt">
    /// Optional UTC time at which the item should be automatically revoked.
    /// Must be in the future when provided.
    /// </param>
    /// <param name="note">Optional note to record in the audit trail.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result{T}"/> containing the updated <see cref="ContentItem{T}"/> on success,
    /// or an error with code <c>Content.NotFound</c>, <c>Content.InvalidTransition</c>,
    /// or <c>ExpiresAt</c> (when the expiry time is in the past) on failure.
    /// </returns>
    Task<Result<ContentItem<T>>> PublishAsync(Guid contentId, string actor, DateTimeOffset? expiresAt = null, string? note = null, CancellationToken ct = default);

    /// <summary>
    /// Schedules an automatic publish for an <see cref="ContentState.Approved"/> item.
    /// The item remains <see cref="ContentState.Approved"/> until the background scheduler picks it up.
    /// </summary>
    /// <param name="contentId">The identifier of the content item to schedule.</param>
    /// <param name="publishAt">UTC time when the item should be automatically published. Must be in the future.</param>
    /// <param name="actor">Identity of the user or service performing the action.</param>
    /// <param name="note">Optional note to record in the audit trail.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result{T}"/> containing the updated <see cref="ContentItem{T}"/> on success,
    /// or an error with code <c>Content.NotFound</c>, <c>Content.SchedulePublishRequiresApproved</c>,
    /// or <c>ScheduledPublishAt</c> (when the scheduled time is in the past) on failure.
    /// </returns>
    Task<Result<ContentItem<T>>> SchedulePublishAsync(Guid contentId, DateTimeOffset publishAt, string actor, string? note = null, CancellationToken ct = default);

    /// <summary>
    /// Transitions an item from any non-terminal state to <see cref="ContentState.Revoked"/>.
    /// </summary>
    /// <param name="contentId">The identifier of the content item to revoke.</param>
    /// <param name="actor">Identity of the user or service performing the action.</param>
    /// <param name="reason">Required reason for revocation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result{T}"/> containing the updated <see cref="ContentItem{T}"/> on success,
    /// or an error with code <c>Content.NotFound</c> or <c>Content.InvalidTransition</c> on failure.
    /// </returns>
    Task<Result<ContentItem<T>>> RevokeAsync(Guid contentId, string actor, string reason, CancellationToken ct = default);

    /// <summary>Returns the content item with the specified <paramref name="contentId"/>.</summary>
    /// <param name="contentId">The identifier of the content item to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result{T}"/> containing the <see cref="ContentItem{T}"/> on success,
    /// or an error with code <c>Content.NotFound</c> if the item does not exist.
    /// </returns>
    Task<Result<ContentItem<T>>> GetAsync(Guid contentId, CancellationToken ct = default);

    /// <summary>Returns all content items currently in the specified <paramref name="state"/>.</summary>
    /// <param name="state">The lifecycle state to filter by.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result{T}"/> containing a read-only list of matching <see cref="ContentItem{T}"/> instances.
    /// Never returns a failure result.
    /// </returns>
    Task<Result<IReadOnlyList<ContentItem<T>>>> GetByStateAsync(ContentState state, CancellationToken ct = default);
}
