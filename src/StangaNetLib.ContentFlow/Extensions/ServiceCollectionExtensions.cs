using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StangaNetLib.ContentFlow.Auditing;
using StangaNetLib.ContentFlow.Configuration;
using StangaNetLib.ContentFlow.Content;
using StangaNetLib.ContentFlow.Workflow;

namespace StangaNetLib.ContentFlow.Extensions;

/// <summary>
/// Extension methods for registering <c>StangaNetLib.ContentFlow</c> services.
/// </summary>
/// <example>
/// appsettings.json:
/// <code>
/// {
///   "ContentFlowSettings": {
///     "ScheduleCheckInterval": "00:00:30",
///     "SchedulerActorName": "system"
///   }
/// }
/// </code>
/// Program.cs:
/// <code>
/// builder.Services.AddStangaNetLibContentFlow(builder.Configuration);
///
/// // Opt in to automatic scheduling for each content type you manage:
/// builder.Services.AddStangaNetLibContentFlowType&lt;Article&gt;();
/// builder.Services.AddStangaNetLibContentFlowType&lt;BlogPost&gt;();
///
/// // Inject and use:
/// // IContentWorkflowService&lt;Article&gt; is resolved automatically via open-generic registration.
///
/// // When using a Scoped store (e.g. EF Core), also register:
/// // services.AddScoped&lt;IContentRepository&lt;Article&gt;, EfArticleRepository&gt;();
/// // services.AddScoped&lt;IContentUnitOfWork, EfContentUnitOfWork&gt;();
/// </code>
/// </example>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <c>StangaNetLib.ContentFlow</c> core services: settings validation, the open-generic
    /// workflow service, the default in-memory repositories, and the background scheduler.
    /// Uses configuration from the <c>ContentFlowSettings</c> section of <paramref name="configuration"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddStangaNetLibContentFlow(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ContentFlowSettings>(configuration.GetSection(ContentFlowSettings.SectionName));
        return RegisterCore(services);
    }

    /// <summary>
    /// Registers <c>StangaNetLib.ContentFlow</c> core services with default settings.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddStangaNetLibContentFlow(this IServiceCollection services)
    {
        services.Configure<ContentFlowSettings>(_ => { });
        return RegisterCore(services);
    }

    /// <summary>
    /// Registers a background schedule processor for content type <typeparamref name="T"/>.
    /// Must be called after <see cref="AddStangaNetLibContentFlow(IServiceCollection, IConfiguration)"/> or <see cref="AddStangaNetLibContentFlow(IServiceCollection)"/>.
    /// The processor runs inside the shared <c>ContentSchedulerService</c> hosted service and handles
    /// automatic publish and expiry revocation for items of type <typeparamref name="T"/>.
    /// Resolved from a DI scope on each tick, so Scoped <see cref="IContentRepository{T}"/> registrations are supported.
    /// </summary>
    /// <typeparam name="T">The content payload type to process on schedule.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddStangaNetLibContentFlowType<T>(this IServiceCollection services)
    {
        services.AddTransient<IContentScheduleProcessor, ContentScheduleProcessor<T>>();
        return services;
    }

    private static IServiceCollection RegisterCore(IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<ContentFlowSettings>, ContentFlowSettingsValidator>();
        services.TryAddSingleton(TimeProvider.System);

        // Open-generic workflow service — new instance per injection point
        services.TryAdd(ServiceDescriptor.Transient(
            typeof(IContentWorkflowService<>),
            typeof(ContentWorkflowService<>)));

        // Default in-memory content repository — one singleton per type T
        services.TryAdd(ServiceDescriptor.Singleton(
            typeof(IContentRepository<>),
            typeof(InMemoryContentRepository<>)));

        // Default in-memory review log repository
        services.TryAddSingleton<IContentReviewLogRepository, InMemoryContentReviewLogRepository>();

        // No-op unit of work — replace with Scoped when using EF Core / similar
        services.TryAddSingleton<IContentUnitOfWork, NullContentUnitOfWork>();

        // Background scheduler — opens a scope each tick and resolves IContentScheduleProcessor instances
        services.AddHostedService<ContentSchedulerService>();

        return services;
    }
}
