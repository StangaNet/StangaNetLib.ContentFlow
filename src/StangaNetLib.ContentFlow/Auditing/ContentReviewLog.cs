using StangaNetLib.ContentFlow.Content;

namespace StangaNetLib.ContentFlow.Auditing;

/// <summary>
/// An immutable audit-trail entry recording a single state transition (or scheduling action)
/// on a content item.
/// </summary>
public sealed record ContentReviewLog
{
    /// <summary>Unique identifier for this log entry.</summary>
    public Guid Id { get; init; }

    /// <summary>Identifier of the content item this entry belongs to.</summary>
    public Guid ContentId { get; init; }

    /// <summary>State before the transition.</summary>
    public ContentState FromState { get; init; }

    /// <summary>
    /// State after the transition.
    /// Equal to <see cref="FromState"/> for scheduling actions that do not change state.
    /// </summary>
    public ContentState ToState { get; init; }

    /// <summary>Identity of the user or service that performed the action.</summary>
    public string Actor { get; init; } = string.Empty;

    /// <summary>Optional note or reason supplied by the actor.</summary>
    public string? Note { get; init; }

    /// <summary>UTC timestamp when this action occurred.</summary>
    public DateTimeOffset OccurredAt { get; init; }
}
