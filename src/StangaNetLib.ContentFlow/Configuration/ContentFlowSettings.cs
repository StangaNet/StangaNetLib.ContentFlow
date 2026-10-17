namespace StangaNetLib.ContentFlow.Configuration;

/// <summary>Configuration for <c>StangaNetLib.ContentFlow</c> services.</summary>
public sealed class ContentFlowSettings
{
    /// <summary>The <c>appsettings.json</c> section key. Value: <c>"ContentFlowSettings"</c>.</summary>
    public const string SectionName = "ContentFlowSettings";

    /// <summary>
    /// How often the background scheduler checks for items due for automatic publish or expiry.
    /// Defaults to 30 seconds.
    /// </summary>
    public TimeSpan ScheduleCheckInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Actor name written into review log entries created by the background scheduler.
    /// Defaults to <c>"system"</c>.
    /// </summary>
    public string SchedulerActorName { get; set; } = "system";
}
