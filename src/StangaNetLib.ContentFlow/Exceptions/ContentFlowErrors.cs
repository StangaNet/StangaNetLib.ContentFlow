using StangaNetLib.ContentFlow.Content;
using StangaNetLib.Core.Common;

namespace StangaNetLib.ContentFlow.Exceptions;

/// <summary>Well-known errors produced by <c>StangaNetLib.ContentFlow</c>.</summary>
public static class ContentFlowErrors
{
    /// <summary>The requested content item does not exist.</summary>
    /// <param name="contentId">The identifier that was not found.</param>
    /// <returns>An <see cref="Error"/> with a <c>NotFound</c> status for the specified content item.</returns>
    public static Error ContentNotFound(Guid contentId)
        => Error.NotFound("Content", contentId);

    /// <summary>The requested state transition is not permitted from the current state.</summary>
    /// <param name="from">The current state of the content item.</param>
    /// <param name="to">The target state that was requested.</param>
    /// <returns>An <see cref="Error"/> with code <c>Content.InvalidTransition</c>.</returns>
    public static Error InvalidTransition(ContentState from, ContentState to)
        => Error.Conflict(
            "Content.InvalidTransition",
            $"Cannot transition from '{from}' to '{to}'.");

    /// <summary>SchedulePublish requires the item to be in the <see cref="ContentState.Approved"/> state.</summary>
    /// <param name="actual">The actual state of the content item at the time of the call.</param>
    /// <returns>An <see cref="Error"/> with code <c>Content.SchedulePublishRequiresApproved</c>.</returns>
    public static Error SchedulePublishRequiresApproved(ContentState actual)
        => Error.Conflict(
            "Content.SchedulePublishRequiresApproved",
            $"SchedulePublish requires the item to be in the Approved state. Current state: '{actual}'.");

    /// <summary>The requested scheduled publish time is in the past.</summary>
    /// <param name="scheduledAt">The invalid scheduled publish time that was provided.</param>
    /// <returns>An <see cref="Error"/> with field code <c>ScheduledPublishAt</c>.</returns>
    public static Error ScheduledTimeInThePast(DateTimeOffset scheduledAt)
        => Error.Validation(
            "ScheduledPublishAt",
            $"Scheduled publish time must be in the future. Received: {scheduledAt:O}.");

    /// <summary>The requested expiry time is in the past.</summary>
    /// <param name="expiresAt">The invalid expiry time that was provided.</param>
    /// <returns>An <see cref="Error"/> with field code <c>ExpiresAt</c>.</returns>
    public static Error ExpiryInThePast(DateTimeOffset expiresAt)
        => Error.Validation(
            "ExpiresAt",
            $"Expiry time must be in the future. Received: {expiresAt:O}.");
}
