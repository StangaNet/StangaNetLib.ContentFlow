namespace StangaNetLib.ContentFlow.Workflow;

/// <summary>Processes a single content type for scheduled publishes and expiry revocations.</summary>
internal interface IContentScheduleProcessor
{
    /// <summary>
    /// Scans for items due for automatic state transition and applies them.
    /// Errors on individual items are swallowed — one bad item does not stop the rest.
    /// </summary>
    Task ProcessDueAsync(CancellationToken ct);
}
