namespace StangaNetLib.ContentFlow.Content;

/// <summary>
/// Persistence abstraction for content items of type <typeparamref name="T"/>.
/// The consuming application provides the concrete implementation (e.g. EF Core).
/// A default in-memory implementation is registered automatically via
/// <c>AddStangaNetLibContentFlow</c>.
/// </summary>
/// <typeparam name="T">The payload type of the managed content items.</typeparam>
public interface IContentRepository<T>
{
    /// <summary>
    /// Returns the content item with the specified <paramref name="id"/>, or <c>null</c> if not found.
    /// </summary>
    /// <param name="id">The identifier of the content item to locate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="ContentItem{T}"/>, or <c>null</c> if no item with that id exists.</returns>
    Task<ContentItem<T>?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all content items currently in the specified <paramref name="state"/>.</summary>
    /// <param name="state">The lifecycle state to filter by.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All items in <paramref name="state"/>. Never null; may be empty.</returns>
    Task<IReadOnlyList<ContentItem<T>>> FindByStateAsync(ContentState state, CancellationToken ct = default);

    /// <summary>
    /// Returns all <see cref="ContentState.Approved"/> items whose
    /// <c>ScheduledPublishAt</c> is non-null and less than or equal to <paramref name="asOf"/>.
    /// </summary>
    /// <param name="asOf">The reference UTC timestamp used to evaluate scheduled publish times.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Items due for automatic publish at or before <paramref name="asOf"/>. Never null; may be empty.</returns>
    Task<IReadOnlyList<ContentItem<T>>> FindScheduledForPublishAsync(DateTimeOffset asOf, CancellationToken ct = default);

    /// <summary>
    /// Returns all <see cref="ContentState.Published"/> items whose
    /// <c>ExpiresAt</c> is non-null and less than or equal to <paramref name="asOf"/>.
    /// </summary>
    /// <param name="asOf">The reference UTC timestamp used to evaluate expiry times.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Items that have expired at or before <paramref name="asOf"/>. Never null; may be empty.</returns>
    Task<IReadOnlyList<ContentItem<T>>> FindExpiredAsync(DateTimeOffset asOf, CancellationToken ct = default);

    /// <summary>Persists a new content item and returns the stored instance.</summary>
    /// <param name="item">The content item to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored <see cref="ContentItem{T}"/> as returned by the persistence layer.</returns>
    Task<ContentItem<T>> AddAsync(ContentItem<T> item, CancellationToken ct = default);

    /// <summary>Replaces the stored content item with <paramref name="item"/> and returns the updated instance.</summary>
    /// <param name="item">The updated content item to persist. Must have an existing <see cref="ContentItem{T}.Id"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored <see cref="ContentItem{T}"/> as returned by the persistence layer.</returns>
    Task<ContentItem<T>> UpdateAsync(ContentItem<T> item, CancellationToken ct = default);
}
