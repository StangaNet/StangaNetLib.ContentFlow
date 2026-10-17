using System.Collections.Concurrent;

namespace StangaNetLib.ContentFlow.Content;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IContentRepository{T}"/>.
/// Registered as the default repository when <c>AddStangaNetLibContentFlow</c> is called.
/// Suitable for development, testing, and single-instance deployments.
/// </summary>
internal sealed class InMemoryContentRepository<T> : IContentRepository<T>
{
    private readonly ConcurrentDictionary<Guid, ContentItem<T>> _store = new();

    public Task<ContentItem<T>?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(id, out var item) ? item : null);

    public Task<IReadOnlyList<ContentItem<T>>> FindByStateAsync(ContentState state, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ContentItem<T>>>(
            [.. _store.Values.Where(i => i.State == state)]);

    public Task<IReadOnlyList<ContentItem<T>>> FindScheduledForPublishAsync(DateTimeOffset asOf, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ContentItem<T>>>(
            [.. _store.Values
                  .Where(i => i.State == ContentState.Approved
                           && i.ScheduledPublishAt.HasValue
                           && i.ScheduledPublishAt.Value <= asOf)]);

    public Task<IReadOnlyList<ContentItem<T>>> FindExpiredAsync(DateTimeOffset asOf, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ContentItem<T>>>(
            [.. _store.Values
                  .Where(i => i.State == ContentState.Published
                           && i.ExpiresAt.HasValue
                           && i.ExpiresAt.Value <= asOf)]);

    public Task<ContentItem<T>> AddAsync(ContentItem<T> item, CancellationToken ct = default)
    {
        if (!_store.TryAdd(item.Id, item))
            throw new InvalidOperationException($"A content item with id '{item.Id}' already exists.");
        return Task.FromResult(item);
    }

    public Task<ContentItem<T>> UpdateAsync(ContentItem<T> item, CancellationToken ct = default)
    {
        _store[item.Id] = item;
        return Task.FromResult(item);
    }
}
