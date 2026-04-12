using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Tests.Domain;

/// <summary>
/// Testes de unidade para a entidade UserAlertRule.
/// Valida criação, invariantes, toggle e actualização de detalhes.
/// </summary>
public sealed class UserAlertRuleTests
{
    private const string ValidCondition = """{"entity":"service","field":"risk_level","operator":">=","value":"high"}""";

    // ── Create — happy path ────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldReturnRule()
    {
        var now = DateTimeOffset.UtcNow;
        var rule = UserAlertRule.Create("user1", "tenant1", "High Risk", ValidCondition, "in-app", now);

        rule.Should().NotBeNull();
        rule.Name.Should().Be("High Risk");
        rule.Channel.Should().Be("in-app");
        rule.IsEnabled.Should().BeTrue();
        rule.CreatedAt.Should().Be(now);
        rule.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("in-app")]
    [InlineData("email")]
    [InlineData("webhook")]
    public void Create_WithAllValidChannels_ShouldSucceed(string channel)
    {
        var rule = UserAlertRule.Create("user1", "tenant1", "Rule", ValidCondition, channel, DateTimeOffset.UtcNow);
        rule.Channel.Should().Be(channel);
    }

    // ── Create — guard clauses ─────────────────────────────────────────

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => UserAlertRule.Create("user1", "tenant1", "", ValidCondition, "in-app", DateTimeOffset.UtcNow);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Create_WithNameOver100Chars_ShouldThrow()
    {
        var longName = new string('x', 101);
        var act = () => UserAlertRule.Create("user1", "tenant1", longName, ValidCondition, "in-app", DateTimeOffset.UtcNow);
        act.Should().Throw<Exception>();
    }

    // ── Channel normalisation ──────────────────────────────────────────

    [Fact]
    public void Create_WithInvalidChannel_ShouldDefaultToInApp()
    {
        var rule = UserAlertRule.Create("user1", "tenant1", "Rule", ValidCondition, "unknown-channel", DateTimeOffset.UtcNow);
        rule.Channel.Should().Be("in-app");
    }

    // ── Toggle ─────────────────────────────────────────────────────────

    [Fact]
    public void Toggle_ShouldChangeEnabledState()
    {
        var now = DateTimeOffset.UtcNow;
        var rule = UserAlertRule.Create("user1", "tenant1", "Test Rule", ValidCondition, "email", now);

        rule.IsEnabled.Should().BeTrue();

        rule.Toggle(false, now.AddMinutes(1));
        rule.IsEnabled.Should().BeFalse();
        rule.UpdatedAt.Should().Be(now.AddMinutes(1));

        rule.Toggle(true, now.AddMinutes(2));
        rule.IsEnabled.Should().BeTrue();
    }

    // ── UpdateDetails ──────────────────────────────────────────────────

    [Fact]
    public void UpdateDetails_ShouldChangeNameConditionAndChannel()
    {
        var now = DateTimeOffset.UtcNow;
        var rule = UserAlertRule.Create("user1", "tenant1", "Old Name", ValidCondition, "email", now);

        var newCondition = """{"entity":"contract","field":"status","operator":"==","value":"expired"}""";
        rule.UpdateDetails("New Name", newCondition, "webhook", now.AddMinutes(1));

        rule.Name.Should().Be("New Name");
        rule.Condition.Should().Be(newCondition);
        rule.Channel.Should().Be("webhook");
        rule.UpdatedAt.Should().Be(now.AddMinutes(1));
    }

    [Fact]
    public void UpdateDetails_WithEmptyName_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var rule = UserAlertRule.Create("user1", "tenant1", "Rule", ValidCondition, "in-app", now);

        var act = () => rule.UpdateDetails("", ValidCondition, "email", now.AddMinutes(1));
        act.Should().Throw<Exception>();
    }
}
