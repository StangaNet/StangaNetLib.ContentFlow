namespace StangaNetLib.ContentFlow.Content;

/// <summary>
/// A managed content item tracked through the lifecycle state machine.
/// Instances are immutable; the workflow service produces updated copies via <c>with</c> expressions.
/// </summary>
/// <typeparam name="T">The payload type representing the actual content data.</typeparam>
public sealed record ContentItem<T>
{
    /// <summary>Unique identifier for this content item.</summary>
    public Guid Id { get; init; }

    /// <summary>Current lifecycle state.</summary>
    public ContentState State { get; init; }

    /// <summary>The content payload supplied by the caller.</summary>
    public T Payload { get; init; } = default!;

    /// <summary>UTC timestamp when this item was first created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>UTC timestamp of the most recent state change.</summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// UTC time at which this item should be automatically published.
    /// Non-null only when <see cref="State"/> is <see cref="ContentState.Approved"/> and a
    /// scheduled publish has been requested via <c>SchedulePublishAsync</c>.
    /// </summary>
    public DateTimeOffset? ScheduledPublishAt { get; init; }

    /// <summary>
    /// UTC time at which this published item should be automatically revoked.
    /// Non-null only when <see cref="State"/> is <see cref="ContentState.Published"/> and an
    /// expiry was specified at publish time.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}
