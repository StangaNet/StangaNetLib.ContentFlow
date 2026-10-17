using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StangaNetLib.ContentFlow.Configuration;

namespace StangaNetLib.ContentFlow.Workflow;

/// <summary>
/// Background service that periodically triggers all registered <see cref="IContentScheduleProcessor"/>
/// instances to publish scheduled items and revoke expired ones.
/// Resolves processors from a DI scope on each tick so Scoped repositories (e.g. EF Core) are supported.
/// </summary>
internal sealed class ContentSchedulerService(
    IServiceScopeFactory scopeFactory,
    IOptions<ContentFlowSettings> settings,
    TimeProvider timeProvider,
    ILogger<ContentSchedulerService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
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

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var processors = scope.ServiceProvider.GetServices<IContentScheduleProcessor>();

                foreach (var processor in processors)
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
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ContentSchedulerService: schedule pass faulted.");
            }
        }
    }
}
