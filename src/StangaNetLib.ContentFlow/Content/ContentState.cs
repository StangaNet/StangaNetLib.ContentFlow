namespace StangaNetLib.ContentFlow.Content;

/// <summary>
/// Represents the lifecycle state of a content item.
/// </summary>
/// <remarks>
/// Valid transitions:
/// <list type="bullet">
///   <item>Draft → Pending (SubmitForReview)</item>
///   <item>Draft → Revoked (Revoke)</item>
///   <item>Pending → Approved (Approve)</item>
///   <item>Pending → Draft (RequestRevision)</item>
///   <item>Pending → Revoked (Revoke)</item>
///   <item>Approved → Published (Publish)</item>
///   <item>Approved → Draft (RequestRevision)</item>
///   <item>Approved → Revoked (Revoke)</item>
///   <item>Published → Revoked (Revoke)</item>
///   <item>Revoked → (terminal — no further transitions)</item>
/// </list>
/// </remarks>
public enum ContentState
{
    /// <summary>Initial state. Content is being authored and is not yet visible.</summary>
    Draft = 0,

    /// <summary>Content has been submitted for review and is awaiting approval.</summary>
    Pending = 1,

    /// <summary>Content has passed review and is ready to be published.</summary>
    Approved = 2,

    /// <summary>Content is live and publicly visible.</summary>
    Published = 3,

    /// <summary>Content has been removed from public view. Terminal state.</summary>
    Revoked = 4,
}
