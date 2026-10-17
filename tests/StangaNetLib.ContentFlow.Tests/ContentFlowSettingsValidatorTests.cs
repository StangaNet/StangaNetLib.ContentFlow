using Microsoft.Extensions.Options;
using StangaNetLib.ContentFlow.Configuration;

namespace StangaNetLib.ContentFlow.Tests;

public sealed class ContentFlowSettingsValidatorTests
{
    private static readonly ContentFlowSettingsValidator Validator = new();

    [Fact]
    public void Validate_DefaultSettings_Passes()
    {
        var result = Validator.Validate(null, new ContentFlowSettings());
        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ScheduleCheckIntervalUnderOneSecond_Fails(int ms)
    {
        var settings = new ContentFlowSettings { ScheduleCheckInterval = TimeSpan.FromMilliseconds(ms) };
        var result = Validator.Validate(null, settings);
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(nameof(ContentFlowSettings.ScheduleCheckInterval));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankSchedulerActorName_Fails(string name)
    {
        var settings = new ContentFlowSettings { SchedulerActorName = name };
        var result = Validator.Validate(null, settings);
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(nameof(ContentFlowSettings.SchedulerActorName));
    }

    [Fact]
    public void Validate_OneSecondInterval_Passes()
    {
        var settings = new ContentFlowSettings { ScheduleCheckInterval = TimeSpan.FromSeconds(1) };
        var result = Validator.Validate(null, settings);
        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_CustomActorName_Passes()
    {
        var settings = new ContentFlowSettings { SchedulerActorName = "scheduler-bot" };
        var result = Validator.Validate(null, settings);
        result.Should().Be(ValidateOptionsResult.Success);
    }
}
