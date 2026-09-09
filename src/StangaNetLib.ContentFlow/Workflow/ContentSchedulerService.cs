using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StangaNetLib.ContentFlow.Configuration;

namespace StangaNetLib.ContentFlow.Workflow;

/// <summary>
/// Background service that periodically triggers all registered <see cref="IContentScheduleProcessor"/>
/// instances to publish scheduled items and revoke expired ones.
/// </summary>
internal sealed class ContentSchedulerService(
    IEnumerable<IContentScheduleProcessor> processors,
    IOptions<ContentFlowSettings> settings,
    TimeProvider timeProvider,
    ILogger<ContentSchedulerService> logger) : BackgroundService
{
    private readonly IEnumerable<IContentScheduleProcessor> _processors = processors;
    private readonly ContentFlowSettings _settings = settings.Value;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<ContentSchedulerService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_settings.ScheduleCheckInterval, _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            foreach (var processor in _processors)
            {
                try
                {
                    await processor.ProcessDueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ContentSchedulerService: processor {Type} faulted.", processor.GetType().Name);
                }
            }
        }
    }
}
