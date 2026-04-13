using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Domain.Entities;

/// <summary>Testes unitários da entidade Release.</summary>
public sealed class ReleaseTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static Release CreateRelease() =>
        Release.Create(Guid.NewGuid(), Guid.Empty, "TestService", "1.0.0", "staging", "https://ci/pipeline/1", "abc123def456", FixedNow);

    [Fact]
    public void Create_ShouldReturnRelease_WithPendingStatus()
    {
        var release = CreateRelease();

        release.Status.Should().Be(DeploymentStatus.Pending);
        release.ServiceName.Should().Be("TestService");
        release.Version.Should().Be("1.0.0");
        release.Environment.Should().Be("staging");
        release.ChangeScore.Should().Be(0m);
    }

    [Fact]
    public void Classify_ShouldUpdateChangeLevel()
    {
        var release = CreateRelease();

        release.Classify(ChangeLevel.Breaking);

        release.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void UpdateStatus_ShouldTransitionFromPendingToRunning()
    {
        var release = CreateRelease();

        var result = release.UpdateStatus(DeploymentStatus.Running);

        result.IsSuccess.Should().BeTrue();
        release.Status.Should().Be(DeploymentStatus.Running);
    }

    [Fact]
    public void UpdateStatus_ShouldFail_WhenInvalidTransition()
    {
        var release = CreateRelease();
        release.UpdateStatus(DeploymentStatus.Running);
        release.UpdateStatus(DeploymentStatus.Failed);

        var result = release.UpdateStatus(DeploymentStatus.Running);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatusTransition");
    }

    [Fact]
    public void SetChangeScore_ShouldSucceed_WithValidScore()
    {
        var release = CreateRelease();

        var result = release.SetChangeScore(0.5m);

        result.IsSuccess.Should().BeTrue();
        release.ChangeScore.Should().Be(0.5m);
    }

    [Fact]
    public void SetChangeScore_ShouldFail_WithScoreAboveOne()
    {
        var release = CreateRelease();

        var result = release.SetChangeScore(1.1m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidChangeScore");
    }

    [Fact]
    public void SetChangeScore_ShouldFail_WithNegativeScore()
    {
        var release = CreateRelease();

        var result = release.SetChangeScore(-0.1m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidChangeScore");
    }

    [Fact]
    public void RegisterRollback_ShouldSucceed_FirstTime()
    {
        var release = CreateRelease();
        release.UpdateStatus(DeploymentStatus.Running);
        release.UpdateStatus(DeploymentStatus.Succeeded);
        var originalId = ReleaseId.New();

        var result = release.RegisterRollback(originalId);

        result.IsSuccess.Should().BeTrue();
        release.RolledBackFromReleaseId.Should().Be(originalId);
        release.Status.Should().Be(DeploymentStatus.RolledBack);
    }

    [Fact]
    public void RegisterRollback_ShouldFail_WhenAlreadyRollback()
    {
        var release = CreateRelease();
        release.UpdateStatus(DeploymentStatus.Running);
        release.UpdateStatus(DeploymentStatus.Succeeded);
        release.RegisterRollback(ReleaseId.New());

        var result = release.RegisterRollback(ReleaseId.New());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyRollback");
    }

    [Fact]
    public void AttachWorkItem_ShouldSetReference()
    {
        var release = CreateRelease();

        release.AttachWorkItem("JIRA-1234");

        release.WorkItemReference.Should().Be("JIRA-1234");
    }
}
