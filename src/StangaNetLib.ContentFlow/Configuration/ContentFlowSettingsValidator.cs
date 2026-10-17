using StangaNetLib.Core.Validators;

namespace StangaNetLib.ContentFlow.Configuration;

/// <summary>
/// Validates <see cref="ContentFlowSettings"/> at application startup.
/// Registered automatically by <c>AddStangaNetLibContentFlow</c>.
/// </summary>
internal sealed class ContentFlowSettingsValidator : SettingsValidatorBase<ContentFlowSettings>
{
    protected override void ValidateCore(ContentFlowSettings options, List<string> errors)
    {
        if (options.ScheduleCheckInterval < TimeSpan.FromSeconds(1))
            errors.Add($"{nameof(ContentFlowSettings.ScheduleCheckInterval)} must be at least 1 second. Received: {options.ScheduleCheckInterval}.");

        if (string.IsNullOrWhiteSpace(options.SchedulerActorName))
            errors.Add($"{nameof(ContentFlowSettings.SchedulerActorName)} must not be null or whitespace.");
    }
}
