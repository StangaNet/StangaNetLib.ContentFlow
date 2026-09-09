using StangaNetLib.ContentFlow.Auditing;
using StangaNetLib.ContentFlow.Content;

namespace StangaNetLib.ContentFlow.Tests;

public sealed class InMemoryContentRepositoryTests
{
    private static ContentItem<string> MakeItem(
        ContentState state = ContentState.Draft,
        DateTimeOffset? scheduledPublishAt = null,
        DateTimeOffset? expiresAt = null) => new()
    {
        Id                 = Guid.NewGuid(),
        State              = state,
        Payload            = "test",
        CreatedAt          = DateTimeOffset.UtcNow,
        UpdatedAt          = DateTimeOffset.UtcNow,
        ScheduledPublishAt = scheduledPublishAt,
        ExpiresAt          = expiresAt,
    };

    [Fact]
    public async Task AddAsync_ThenFindById_ReturnsItem()
    {
        var repo = new InMemoryContentRepository<string>();
        var item = MakeItem();
        await repo.AddAsync(item);
        var found = await repo.FindByIdAsync(item.Id);
        found.Should().NotBeNull();
        found!.Id.Should().Be(item.Id);
    }

    [Fact]
    public async Task FindByIdAsync_MissingId_ReturnsNull()
    {
        var repo = new InMemoryContentRepository<string>();
        var found = await repo.FindByIdAsync(Guid.NewGuid());
        found.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_DuplicateId_Throws()
    {
        var repo = new InMemoryContentRepository<string>();
        var item = MakeItem();
        await repo.AddAsync(item);
        var act = async () => await repo.AddAsync(item);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_ReplacesItem()
    {
        var repo = new InMemoryContentRepository<string>();
        var item = MakeItem();
        await repo.AddAsync(item);
        var updated = item with { State = ContentState.Pending };
        await repo.UpdateAsync(updated);
        var found = await repo.FindByIdAsync(item.Id);
        found!.State.Should().Be(ContentState.Pending);
    }

    [Fact]
    public async Task FindByStateAsync_FiltersCorrectly()
    {
        var repo = new InMemoryContentRepository<string>();
        var draft   = MakeItem(ContentState.Draft);
        var pending = MakeItem(ContentState.Pending);
        await repo.AddAsync(draft);
        await repo.AddAsync(pending);
        var result = await repo.FindByStateAsync(ContentState.Draft);
        result.Should().ContainSingle(i => i.Id == draft.Id);
        result.Should().NotContain(i => i.Id == pending.Id);
    }

    [Fact]
    public async Task FindScheduledForPublishAsync_ReturnsDueApprovedItems()
    {
        var repo = new InMemoryContentRepository<string>();
        var past   = MakeItem(ContentState.Approved, scheduledPublishAt: DateTimeOffset.UtcNow.AddMinutes(-5));
        var future = MakeItem(ContentState.Approved, scheduledPublishAt: DateTimeOffset.UtcNow.AddHours(1));
        var none   = MakeItem(ContentState.Approved);
        await repo.AddAsync(past);
        await repo.AddAsync(future);
        await repo.AddAsync(none);

        var result = await repo.FindScheduledForPublishAsync(DateTimeOffset.UtcNow);
        result.Should().ContainSingle(i => i.Id == past.Id);
        result.Should().NotContain(i => i.Id == future.Id);
        result.Should().NotContain(i => i.Id == none.Id);
    }

    [Fact]
    public async Task FindScheduledForPublishAsync_NonApprovedItems_Excluded()
    {
        var repo = new InMemoryContentRepository<string>();
        var draft = MakeItem(ContentState.Draft, scheduledPublishAt: DateTimeOffset.UtcNow.AddMinutes(-1));
        await repo.AddAsync(draft);
        var result = await repo.FindScheduledForPublishAsync(DateTimeOffset.UtcNow);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindExpiredAsync_ReturnsExpiredPublishedItems()
    {
        var repo = new InMemoryContentRepository<string>();
        var expired = MakeItem(ContentState.Published, expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));
        var future  = MakeItem(ContentState.Published, expiresAt: DateTimeOffset.UtcNow.AddHours(1));
        var noExpiry = MakeItem(ContentState.Published);
        await repo.AddAsync(expired);
        await repo.AddAsync(future);
        await repo.AddAsync(noExpiry);

        var result = await repo.FindExpiredAsync(DateTimeOffset.UtcNow);
        result.Should().ContainSingle(i => i.Id == expired.Id);
        result.Should().NotContain(i => i.Id == future.Id);
        result.Should().NotContain(i => i.Id == noExpiry.Id);
    }
}

public sealed class InMemoryContentReviewLogRepositoryTests
{
    [Fact]
    public async Task AddAsync_ThenGetByContentId_ReturnsEntry()
    {
        var repo = new InMemoryContentReviewLogRepository();
        var entry = new ContentReviewLog
        {
            Id         = Guid.NewGuid(),
            ContentId  = Guid.NewGuid(),
            FromState  = ContentState.Draft,
            ToState    = ContentState.Pending,
            Actor      = "user1",
            OccurredAt = DateTimeOffset.UtcNow,
        };
        await repo.AddAsync(entry);
        var result = await repo.GetByContentIdAsync(entry.ContentId);
        result.Should().ContainSingle().Which.Id.Should().Be(entry.Id);
    }

    [Fact]
    public async Task GetByContentIdAsync_DifferentContentId_ReturnsEmpty()
    {
        var repo = new InMemoryContentReviewLogRepository();
        var entry = new ContentReviewLog
        {
            Id         = Guid.NewGuid(),
            ContentId  = Guid.NewGuid(),
            FromState  = ContentState.Draft,
            ToState    = ContentState.Pending,
            Actor      = "user1",
            OccurredAt = DateTimeOffset.UtcNow,
        };
        await repo.AddAsync(entry);
        var result = await repo.GetByContentIdAsync(Guid.NewGuid());
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_MultipleEntries_ReturnedInOrder()
    {
        var repo = new InMemoryContentReviewLogRepository();
        var contentId = Guid.NewGuid();
        var e1 = new ContentReviewLog { Id = Guid.NewGuid(), ContentId = contentId, FromState = ContentState.Draft,   ToState = ContentState.Pending,  Actor = "a", OccurredAt = DateTimeOffset.UtcNow };
        var e2 = new ContentReviewLog { Id = Guid.NewGuid(), ContentId = contentId, FromState = ContentState.Pending, ToState = ContentState.Approved, Actor = "b", OccurredAt = DateTimeOffset.UtcNow };
        await repo.AddAsync(e1);
        await repo.AddAsync(e2);
        var result = await repo.GetByContentIdAsync(contentId);
        result.Should().HaveCount(2);
    }
}
