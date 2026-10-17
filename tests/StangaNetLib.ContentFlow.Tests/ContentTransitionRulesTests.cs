using StangaNetLib.ContentFlow.Workflow;
using StangaNetLib.ContentFlow.Content;

namespace StangaNetLib.ContentFlow.Tests;

public sealed class ContentTransitionRulesTests
{
    [Theory]
    [InlineData(ContentState.Draft,     ContentState.Pending)]
    [InlineData(ContentState.Draft,     ContentState.Revoked)]
    [InlineData(ContentState.Pending,   ContentState.Approved)]
    [InlineData(ContentState.Pending,   ContentState.Draft)]
    [InlineData(ContentState.Pending,   ContentState.Revoked)]
    [InlineData(ContentState.Approved,  ContentState.Published)]
    [InlineData(ContentState.Approved,  ContentState.Draft)]
    [InlineData(ContentState.Approved,  ContentState.Revoked)]
    [InlineData(ContentState.Published, ContentState.Revoked)]
    public void IsAllowed_ValidTransition_ReturnsTrue(ContentState from, ContentState to)
        => ContentTransitionRules.IsAllowed(from, to).Should().BeTrue();

    [Theory]
    [InlineData(ContentState.Draft,     ContentState.Approved)]
    [InlineData(ContentState.Draft,     ContentState.Published)]
    [InlineData(ContentState.Pending,   ContentState.Published)]
    [InlineData(ContentState.Approved,  ContentState.Pending)]
    [InlineData(ContentState.Published, ContentState.Draft)]
    [InlineData(ContentState.Published, ContentState.Approved)]
    [InlineData(ContentState.Published, ContentState.Pending)]
    [InlineData(ContentState.Revoked,   ContentState.Draft)]
    [InlineData(ContentState.Revoked,   ContentState.Pending)]
    [InlineData(ContentState.Revoked,   ContentState.Approved)]
    [InlineData(ContentState.Revoked,   ContentState.Published)]
    public void IsAllowed_InvalidTransition_ReturnsFalse(ContentState from, ContentState to)
        => ContentTransitionRules.IsAllowed(from, to).Should().BeFalse();

    [Fact]
    public void IsAllowed_SameState_ReturnsFalse()
    {
        foreach (ContentState state in Enum.GetValues<ContentState>())
            ContentTransitionRules.IsAllowed(state, state).Should().BeFalse();
    }

    [Fact]
    public void AllowedTargets_Revoked_ReturnsEmptySet()
        => ContentTransitionRules.AllowedTargets(ContentState.Revoked).Should().BeEmpty();

    [Fact]
    public void AllowedTargets_Draft_ContainsPendingAndRevoked()
        => ContentTransitionRules.AllowedTargets(ContentState.Draft)
            .Should().BeEquivalentTo(new[] { ContentState.Pending, ContentState.Revoked });
}
