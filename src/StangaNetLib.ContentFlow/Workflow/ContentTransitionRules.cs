using StangaNetLib.ContentFlow.Content;

namespace StangaNetLib.ContentFlow.Workflow;

/// <summary>Encodes the legal state transitions for the content lifecycle state machine.</summary>
internal static class ContentTransitionRules
{
    private static readonly IReadOnlyDictionary<ContentState, IReadOnlySet<ContentState>> Allowed =
        new Dictionary<ContentState, IReadOnlySet<ContentState>>
        {
            [ContentState.Draft]     = new HashSet<ContentState> { ContentState.Pending, ContentState.Revoked },
            [ContentState.Pending]   = new HashSet<ContentState> { ContentState.Approved, ContentState.Draft, ContentState.Revoked },
            [ContentState.Approved]  = new HashSet<ContentState> { ContentState.Published, ContentState.Draft, ContentState.Revoked },
            [ContentState.Published] = new HashSet<ContentState> { ContentState.Revoked },
            [ContentState.Revoked]   = new HashSet<ContentState>(),
        };

    /// <summary>Returns <c>true</c> when transitioning from <paramref name="from"/> to <paramref name="to"/> is legal.</summary>
    public static bool IsAllowed(ContentState from, ContentState to)
        => Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    /// <summary>Returns the set of states reachable from <paramref name="from"/>.</summary>
    public static IReadOnlySet<ContentState> AllowedTargets(ContentState from)
        => Allowed.TryGetValue(from, out var targets) ? targets : new HashSet<ContentState>();
}
