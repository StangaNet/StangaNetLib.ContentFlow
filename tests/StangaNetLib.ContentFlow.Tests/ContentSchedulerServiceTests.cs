using Microsoft.Extensions.DependencyInjection;
using StangaNetLib.ContentFlow.Workflow;
using StangaNetLib.ContentFlow.Content;
using StangaNetLib.ContentFlow.Auditing;

namespace StangaNetLib.ContentFlow.Tests;

public sealed class ContentSchedulerServiceTests
{
    private static ContentScheduleProcessor<string> BuildProcessor(
        InMemoryContentRepository<string> repo,
        InMemoryContentReviewLogRepository logRepo,
        TimeProvider timeProvider) =>
        new(repo, logRepo, timeProvider, "system");

    private static ContentItem<string> MakeItem(
        ContentState state,
        DateTimeOffset? scheduledPublishAt = null,
        DateTimeOffset? expiresAt = null) => new()
    {
        Id                 = Guid.NewGuid(),
        State              = state,
        Payload            = "payload",
        CreatedAt          = DateTimeOffset.UtcNow,
        UpdatedAt          = DateTimeOffset.UtcNow,
        ScheduledPublishAt = scheduledPublishAt,
        ExpiresAt          = expiresAt,
    };

    [Fact]
    public async Task ProcessDueAsync_ScheduledApprovedItem_PublishesIt()
    {
        var repo    = new InMemoryContentRepository<string>();
        var logRepo = new InMemoryContentReviewLogRepository();
        var tp      = TimeProvider.System;

        var item = MakeItem(ContentState.Approved, scheduledPublishAt: DateTimeOffset.UtcNow.AddMinutes(-1));
        await repo.AddAsync(item);

        var processor = BuildProcessor(repo, logRepo, tp);
        await processor.ProcessDueAsync(CancellationToken.None);

        var updated = await repo.FindByIdAsync(item.Id);
        updated!.State.Should().Be(ContentState.Published);
        updated.ScheduledPublishAt.Should().BeNull();
    }

    [Fact]
    public async Task ProcessDueAsync_ScheduledApprovedItem_AppendsPublishLog()
    {
        var repo    = new InMemoryContentRepository<string>();
        var logRepo = new InMemoryContentReviewLogRepository();
        var tp      = TimeProvider.System;

        var item = MakeItem(ContentState.Approved, scheduledPublishAt: DateTimeOffset.UtcNow.AddMinutes(-1));
        await repo.AddAsync(item);

        var processor = BuildProcessor(repo, logRepo, tp);
        await processor.ProcessDueAsync(CancellationToken.None);

        var logs = await logRepo.GetByContentIdAsync(item.Id);
        logs.Should().ContainSingle();
        logs[0].FromState.Should().Be(ContentState.Approved);
        logs[0].ToState.Should().Be(ContentState.Published);
        logs[0].Actor.Should().Be("system");
    }

    [Fact]
    public async Task ProcessDueAsync_FutureScheduledItem_NotPublished()
    {
        var repo    = new InMemoryContentRepository<string>();
        var logRepo = new InMemoryContentReviewLogRepository();
        var tp      = TimeProvider.System;

        var item = MakeItem(ContentState.Approved, scheduledPublishAt: DateTimeOffset.UtcNow.AddHours(1));
        await repo.AddAsync(item);

        var processor = BuildProcessor(repo, logRepo, tp);
        await processor.ProcessDueAsync(CancellationToken.None);

        var updated = await repo.FindByIdAsync(item.Id);
        updated!.State.Should().Be(ContentState.Approved);
    }

    [Fact]
    public async Task ProcessDueAsync_ExpiredPublishedItem_RevokesIt()
    {
        var repo    = new InMemoryContentRepository<string>();
        var logRepo = new InMemoryContentReviewLogRepository();
        var tp      = TimeProvider.System;

        var item = MakeItem(ContentState.Published, expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));
        await repo.AddAsync(item);

        var processor = BuildProcessor(repo, logRepo, tp);
        await processor.ProcessDueAsync(CancellationToken.None);

        var updated = await repo.FindByIdAsync(item.Id);
        updated!.State.Should().Be(ContentState.Revoked);
    }

    [Fact]
    public async Task ProcessDueAsync_ExpiredPublishedItem_AppendsRevokeLog()
    {
        var repo    = new InMemoryContentRepository<string>();
        var logRepo = new InMemoryContentReviewLogRepository();
        var tp      = TimeProvider.System;

        var item = MakeItem(ContentState.Published, expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));
        await repo.AddAsync(item);

        var processor = BuildProcessor(repo, logRepo, tp);
        await processor.ProcessDueAsync(CancellationToken.None);

        var logs = await logRepo.GetByContentIdAsync(item.Id);
        logs.Should().ContainSingle();
        logs[0].FromState.Should().Be(ContentState.Published);
        logs[0].ToState.Should().Be(ContentState.Revoked);
    }

    [Fact]
    public async Task ProcessDueAsync_NoItems_CompletesWithoutError()
    {
        var repo    = new InMemoryContentRepository<string>();
        var logRepo = new InMemoryContentReviewLogRepository();
        var processor = BuildProcessor(repo, logRepo, TimeProvider.System);
        var act = async () => await processor.ProcessDueAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
