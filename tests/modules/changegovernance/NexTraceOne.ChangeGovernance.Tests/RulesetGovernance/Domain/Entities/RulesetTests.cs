using NexTraceOne.RulesetGovernance.Domain.Entities;

namespace NexTraceOne.RulesetGovernance.Tests.Domain.Entities;

/// <summary>Testes unitarios da entidade Ruleset.</summary>
public sealed class RulesetTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_ReturnRuleset_WithCorrectProperties()
    {
        var ruleset = Ruleset.Create("Test Ruleset", "A test description", "{}", RulesetType.Custom, FixedNow);

        ruleset.Should().NotBeNull();
        ruleset.Id.Value.Should().NotBeEmpty();
        ruleset.Name.Should().Be("Test Ruleset");
        ruleset.Description.Should().Be("A test description");
        ruleset.Content.Should().Be("{}");
        ruleset.RulesetType.Should().Be(RulesetType.Custom);
        ruleset.IsActive.Should().BeTrue();
        ruleset.RulesetCreatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void Create_Should_SetDefaultRulesetType()
    {
        var ruleset = Ruleset.Create("Default Rules", "Default description", "{}", RulesetType.Default, FixedNow);

        ruleset.RulesetType.Should().Be(RulesetType.Default);
    }

    [Fact]
    public void Archive_Should_SetIsActiveToFalse_WhenActive()
    {
        var ruleset = Ruleset.Create("Test", "Desc", "{}", RulesetType.Custom, FixedNow);

        var result = ruleset.Archive();

        result.IsSuccess.Should().BeTrue();
        ruleset.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Archive_Should_ReturnError_WhenAlreadyArchived()
    {
        var ruleset = Ruleset.Create("Test", "Desc", "{}", RulesetType.Custom, FixedNow);
        ruleset.Archive();

        var result = ruleset.Archive();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyArchived");
    }

    [Fact]
    public void Activate_Should_SetIsActiveToTrue()
    {
        var ruleset = Ruleset.Create("Test", "Desc", "{}", RulesetType.Custom, FixedNow);
        ruleset.Archive();

        ruleset.Activate();

        ruleset.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateContent_Should_ChangeContent()
    {
        var ruleset = Ruleset.Create("Test", "Desc", "{}", RulesetType.Custom, FixedNow);

        ruleset.UpdateContent("{\"rules\": {}}");

        ruleset.Content.Should().Be("{\"rules\": {}}");
    }

    [Fact]
    public void Create_Should_ThrowOnNullName()
    {
        var act = () => Ruleset.Create(null!, "Desc", "{}", RulesetType.Custom, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_ThrowOnNullContent()
    {
        var act = () => Ruleset.Create("Test", "Desc", null!, RulesetType.Custom, FixedNow);

        act.Should().Throw<ArgumentException>();
    }
}
