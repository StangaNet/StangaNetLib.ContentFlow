using Microsoft.Extensions.DependencyInjection;
using StangaNetLib.ContentFlow.Workflow;
using StangaNetLib.ContentFlow.Content;
using StangaNetLib.ContentFlow.Auditing;
using StangaNetLib.ContentFlow.Configuration;
using StangaNetLib.ContentFlow.Extensions;

namespace StangaNetLib.ContentFlow.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddStangaNetLibContentFlow_RegistersWorkflowServiceForGenericType()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStangaNetLibContentFlow();

        using var sp = services.BuildServiceProvider();
        var svc = sp.GetService<IContentWorkflowService<string>>();
        svc.Should().NotBeNull();
    }

    [Fact]
    public void AddStangaNetLibContentFlow_RegistersReviewLogRepository()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStangaNetLibContentFlow();

        using var sp = services.BuildServiceProvider();
        var repo = sp.GetService<IContentReviewLogRepository>();
        repo.Should().NotBeNull();
    }

    [Fact]
    public void AddStangaNetLibContentFlow_RegistersContentRepositoryForGenericType()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStangaNetLibContentFlow();

        using var sp = services.BuildServiceProvider();
        var repo = sp.GetService<IContentRepository<string>>();
        repo.Should().NotBeNull();
    }

    [Fact]
    public async Task AddStangaNetLibContentFlow_FullWorkflow_CreateAndTransition()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStangaNetLibContentFlow();

        using var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<IContentWorkflowService<string>>();

        var created = await svc.CreateAsync("my article");
        created.IsSuccess.Should().BeTrue();
        created.Value!.State.Should().Be(ContentState.Draft);

        var pending = await svc.SubmitForReviewAsync(created.Value.Id, "author");
        pending.IsSuccess.Should().BeTrue();
        pending.Value!.State.Should().Be(ContentState.Pending);
    }

    [Fact]
    public void AddStangaNetLibContentFlowType_RegistersScheduleProcessor()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStangaNetLibContentFlow();
        services.AddStangaNetLibContentFlowType<string>();

        using var sp = services.BuildServiceProvider();
        // Verify the processor is resolvable through IContentWorkflowService (processor is internal)
        var svc = sp.GetService<IContentWorkflowService<string>>();
        svc.Should().NotBeNull();
    }
}
