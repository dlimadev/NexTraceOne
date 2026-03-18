using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Domain;

/// <summary>Testes unitários da entidade AiSource.</summary>
public sealed class AiSourceTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_WithValidData_ShouldCreateSource()
    {
        var source = AiSource.Register(
            "confluence-eng", "Confluence Engineering", AiSourceType.Document,
            "Engineering wiki", "https://confluence.local/eng",
            "team:platform", "internal", "platform-team", FixedNow);

        source.Name.Should().Be("confluence-eng");
        source.DisplayName.Should().Be("Confluence Engineering");
        source.SourceType.Should().Be(AiSourceType.Document);
        source.Description.Should().Be("Engineering wiki");
        source.IsEnabled.Should().BeTrue();
        source.ConnectionInfo.Should().Be("https://confluence.local/eng");
        source.AccessPolicyScope.Should().Be("team:platform");
        source.Classification.Should().Be("internal");
        source.OwnerTeam.Should().Be("platform-team");
        source.HealthStatus.Should().Be("Unknown");
        source.RegisteredAt.Should().Be(FixedNow);
    }

    [Fact]
    public void Register_WithNullName_ShouldThrow()
    {
        var act = () => AiSource.Register(
            null!, "Display", AiSourceType.Database,
            "desc", "conn", "scope", "class", "team", FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Enable_ShouldSetIsEnabled()
    {
        var source = AiSource.Register(
            "s", "S", AiSourceType.Telemetry, "d", "c", "sc", "cl", "t", FixedNow);
        source.Disable();

        var result = source.Enable();

        result.IsSuccess.Should().BeTrue();
        source.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Disable_ShouldSetIsEnabled()
    {
        var source = AiSource.Register(
            "s", "S", AiSourceType.Telemetry, "d", "c", "sc", "cl", "t", FixedNow);

        var result = source.Disable();

        result.IsSuccess.Should().BeTrue();
        source.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void UpdateHealth_ShouldUpdateStatus()
    {
        var source = AiSource.Register(
            "s", "S", AiSourceType.Database, "d", "c", "sc", "cl", "t", FixedNow);
        source.HealthStatus.Should().Be("Unknown");

        var result = source.UpdateHealth("Healthy");

        result.IsSuccess.Should().BeTrue();
        source.HealthStatus.Should().Be("Healthy");
    }
}
