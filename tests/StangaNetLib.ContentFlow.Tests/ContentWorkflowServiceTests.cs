using StangaNetLib.ContentFlow.Workflow;
using StangaNetLib.ContentFlow.Content;
using StangaNetLib.Core.Common;
using StangaNetLib.ContentFlow.Auditing;

namespace StangaNetLib.ContentFlow.Tests;

public sealed class ContentWorkflowServiceTests
{
    private static IContentWorkflowService<string> Build(
        InMemoryContentRepository<string>? repo = null,
        InMemoryContentReviewLogRepository? logRepo = null,
        TimeProvider? timeProvider = null)
        => new ContentWorkflowService<string>(
            repo        ?? new InMemoryContentRepository<string>(),
            logRepo     ?? new InMemoryContentReviewLogRepository(),
            timeProvider ?? TimeProvider.System);

    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidPayload_ReturnsSuccessDraftItem()
    {
        var svc = Build();
        var result = await svc.CreateAsync("hello");
        result.IsSuccess.Should().BeTrue();
        result.Value!.State.Should().Be(ContentState.Draft);
        result.Value.Payload.Should().Be("hello");
        result.Value.Id.Should().NotBe(Guid.Empty);
        result.Value.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreateAsync_TwoCalls_ReturnDistinctIds()
    {
        var svc = Build();
        var a = await svc.CreateAsync("a");
        var b = await svc.CreateAsync("b");
        a.Value!.Id.Should().NotBe(b.Value!.Id);
    }

    // ── SubmitForReview ──────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitForReview_FromDraft_TransitionsToPending()
    {
        var svc = Build();
        var created = (await svc.CreateAsync("content")).Value!;
        var result = await svc.SubmitForReviewAsync(created.Id, "author");
        result.IsSuccess.Should().BeTrue();
        result.Value!.State.Should().Be(ContentState.Pending);
    }

    [Theory]
    [InlineData(ContentState.Pending)]
    [InlineData(ContentState.Approved)]
    [InlineData(ContentState.Published)]
    [InlineData(ContentState.Revoked)]
    public async Task SubmitForReview_FromNonDraft_ReturnsConflictError(ContentState initial)
    {
        var (svc, id) = await ReachStateAsync(initial);
        var result = await svc.SubmitForReviewAsync(id, "author");
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    // ── Approve ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Approve_FromPending_TransitionsToApproved()
    {
        var (svc, id) = await ReachStateAsync(ContentState.Pending);
        var result = await svc.ApproveAsync(id, "reviewer");
        result.IsSuccess.Should().BeTrue();
        result.Value!.State.Should().Be(ContentState.Approved);
    }

    [Theory]
    [InlineData(ContentState.Draft)]
    [InlineData(ContentState.Approved)]
    [InlineData(ContentState.Published)]
    [InlineData(ContentState.Revoked)]
    public async Task Approve_FromNonPending_ReturnsConflictError(ContentState initial)
    {
        var (svc, id) = await ReachStateAsync(initial);
        var result = await svc.ApproveAsync(id, "reviewer");
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    // ── RequestRevision ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(ContentState.Pending)]
    [InlineData(ContentState.Approved)]
    public async Task RequestRevision_FromPendingOrApproved_TransitionsToDraft(ContentState initial)
    {
        var (svc, id) = await ReachStateAsync(initial);
        var result = await svc.RequestRevisionAsync(id, "reviewer", "Needs more details.");
        result.IsSuccess.Should().BeTrue();
        result.Value!.State.Should().Be(ContentState.Draft);
    }

    [Fact]
    public async Task RequestRevision_ClearsScheduledPublishAt()
    {
        var (svc, id) = await ReachStateAsync(ContentState.Approved);
        await svc.SchedulePublishAsync(id, DateTimeOffset.UtcNow.AddHours(1), "editor");
        var result = await svc.RequestRevisionAsync(id, "reviewer", "Revision required.");
        result.Value!.ScheduledPublishAt.Should().BeNull();
    }

    [Theory]
    [InlineData(ContentState.Published)]
    [InlineData(ContentState.Revoked)]
    public async Task RequestRevision_FromTerminalOrPublished_ReturnsConflictError(ContentState initial)
    {
        var (svc, id) = await ReachStateAsync(initial);
        var result = await svc.RequestRevisionAsync(id, "reviewer", "note");
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    // ── Publish ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Publish_FromApproved_TransitionsToPublished()
    {
        var (svc, id) = await ReachStateAsync(ContentState.Approved);
        var result = await svc.PublishAsync(id, "editor");
        result.IsSuccess.Should().BeTrue();
        result.Value!.State.Should().Be(ContentState.Published);
        result.Value.ScheduledPublishAt.Should().BeNull();
    }

    [Fact]
    public async Task Publish_WithFutureExpiry_SetsExpiresAt()
    {
        var (svc, id) = await ReachStateAsync(ContentState.Approved);
        var expiry = DateTimeOffset.UtcNow.AddDays(7);
        var result = await svc.PublishAsync(id, "editor", expiresAt: expiry);
        result.IsSuccess.Should().BeTrue();
        result.Value!.ExpiresAt.Should().BeCloseTo(expiry, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Publish_WithPastExpiry_ReturnsValidationError()
    {
        var (svc, id) = await ReachStateAsync(ContentState.Approved);
        var result = await svc.PublishAsync(id, "editor", expiresAt: DateTimeOffset.UtcNow.AddHours(-1));
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Theory]
    [InlineData(ContentState.Draft)]
    [InlineData(ContentState.Pending)]
    [InlineData(ContentState.Published)]
    [InlineData(ContentState.Revoked)]
    public async Task Publish_FromNonApproved_ReturnsConflictError(ContentState initial)
    {
        var (svc, id) = await ReachStateAsync(initial);
        var result = await svc.PublishAsync(id, "editor");
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    // ── SchedulePublish ──────────────────────────────────────────────────────

    [Fact]
    public async Task SchedulePublish_FromApproved_SetsScheduledPublishAtAndKeepsApproved()
    {
        var (svc, id) = await ReachStateAsync(ContentState.Approved);
        var publishAt = DateTimeOffset.UtcNow.AddDays(1);
        var result = await svc.SchedulePublishAsync(id, publishAt, "editor");
        result.IsSuccess.Should().BeTrue();
        result.Value!.State.Should().Be(ContentState.Approved);
        result.Value.ScheduledPublishAt.Should().BeCloseTo(publishAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SchedulePublish_WithPastTime_ReturnsValidationError()
    {
        var (svc, id) = await ReachStateAsync(ContentState.Approved);
        var result = await svc.SchedulePublishAsync(id, DateTimeOffset.UtcNow.AddHours(-1), "editor");
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Theory]
    [InlineData(ContentState.Draft)]
    [InlineData(ContentState.Pending)]
    [InlineData(ContentState.Published)]
    [InlineData(ContentState.Revoked)]
    public async Task SchedulePublish_FromNonApproved_ReturnsConflictError(ContentState initial)
    {
        var (svc, id) = await ReachStateAsync(initial);
        var result = await svc.SchedulePublishAsync(id, DateTimeOffset.UtcNow.AddDays(1), "editor");
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    // ── Revoke ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ContentState.Draft)]
    [InlineData(ContentState.Pending)]
    [InlineData(ContentState.Approved)]
    [InlineData(ContentState.Published)]
    public async Task Revoke_FromAnyNonTerminalState_TransitionsToRevoked(ContentState initial)
    {
        var (svc, id) = await ReachStateAsync(initial);
        var result = await svc.RevokeAsync(id, "admin", "Policy violation.");
        result.IsSuccess.Should().BeTrue();
        result.Value!.State.Should().Be(ContentState.Revoked);
    }

    [Fact]
    public async Task Revoke_FromRevoked_ReturnsConflictError()
    {
        var (svc, id) = await ReachStateAsync(ContentState.Revoked);
        var result = await svc.RevokeAsync(id, "admin", "reason");
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    // ── Get ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_ExistingItem_ReturnsItem()
    {
        var svc = Build();
        var created = (await svc.CreateAsync("data")).Value!;
        var result = await svc.GetAsync(created.Id);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetAsync_MissingItem_ReturnsNotFoundError()
    {
        var svc = Build();
        var result = await svc.GetAsync(Guid.NewGuid());
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetByStateAsync_FiltersCorrectly()
    {
        var svc = Build();
        await svc.CreateAsync("draft1");
        var pending = (await svc.CreateAsync("pending1")).Value!;
        await svc.SubmitForReviewAsync(pending.Id, "author");

        var result = await svc.GetByStateAsync(ContentState.Pending);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(1);
        result.Value![0].Id.Should().Be(pending.Id);
    }

    // ── Audit log ────────────────────────────────────────────────────────────

    [Fact]
    public async Task TransitionAsync_AppendsReviewLogEntry()
    {
        var repo    = new InMemoryContentRepository<string>();
        var logRepo = new InMemoryContentReviewLogRepository();
        var svc = Build(repo, logRepo);

        var created = (await svc.CreateAsync("content")).Value!;
        await svc.SubmitForReviewAsync(created.Id, "author", "ready");

        var logs = await logRepo.GetByContentIdAsync(created.Id);
        logs.Should().HaveCount(1);
        logs[0].FromState.Should().Be(ContentState.Draft);
        logs[0].ToState.Should().Be(ContentState.Pending);
        logs[0].Actor.Should().Be("author");
        logs[0].Note.Should().Be("ready");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static async Task<(IContentWorkflowService<string> svc, Guid id)> ReachStateAsync(ContentState target)
    {
        var svc = Build();
        var item = (await svc.CreateAsync("payload")).Value!;
        var id = item.Id;

        if (target == ContentState.Draft)    return (svc, id);
        await svc.SubmitForReviewAsync(id, "author");
        if (target == ContentState.Pending)  return (svc, id);
        await svc.ApproveAsync(id, "reviewer");
        if (target == ContentState.Approved) return (svc, id);
        if (target == ContentState.Published) { await svc.PublishAsync(id, "editor"); return (svc, id); }
        // Revoked from Approved (direct valid path)
        await svc.RevokeAsync(id, "admin", "test");
        return (svc, id);
    }
}
