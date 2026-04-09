using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Domain.Entities;

public sealed class OnboardingSessionTests
{
    // ── Factory method: valid creation ───────────────────────────────────

    [Fact]
    public void Create_With_Valid_Data_Returns_Session()
    {
        var teamId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var session = OnboardingSession.Create(
            userId: "user-1",
            userDisplayName: "Alice Smith",
            teamId: teamId,
            teamName: "Platform Team",
            experienceLevel: OnboardingExperienceLevel.Mid,
            checklistItems: "[\"item1\",\"item2\"]",
            totalItems: 5,
            tenantId: tenantId,
            startedAt: now);

        session.Should().NotBeNull();
        session.Id.Value.Should().NotBeEmpty();
        session.UserId.Should().Be("user-1");
        session.UserDisplayName.Should().Be("Alice Smith");
        session.TeamId.Should().Be(teamId);
        session.TeamName.Should().Be("Platform Team");
        session.ExperienceLevel.Should().Be(OnboardingExperienceLevel.Mid);
        session.Status.Should().Be(OnboardingSessionStatus.Active);
        session.ChecklistItems.Should().Be("[\"item1\",\"item2\"]");
        session.CompletedItems.Should().Be(0);
        session.TotalItems.Should().Be(5);
        session.ProgressPercent.Should().Be(0);
        session.ServicesExplored.Should().BeNull();
        session.ContractsReviewed.Should().BeNull();
        session.RunbooksRead.Should().BeNull();
        session.AiInteractionCount.Should().Be(0);
        session.StartedAt.Should().Be(now);
        session.CompletedAt.Should().BeNull();
        session.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_Generates_Unique_Ids()
    {
        var s1 = CreateValidSession();
        var s2 = CreateValidSession();
        s1.Id.Should().NotBe(s2.Id);
    }

    // ── Guard clause validation ─────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_UserId(string? userId)
    {
        var act = () => OnboardingSession.Create(
            userId: userId!,
            userDisplayName: "Name",
            teamId: Guid.NewGuid(),
            teamName: "Team",
            experienceLevel: OnboardingExperienceLevel.Junior,
            checklistItems: "[]",
            totalItems: 1,
            tenantId: Guid.NewGuid(),
            startedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_UserDisplayName(string? displayName)
    {
        var act = () => OnboardingSession.Create(
            userId: "user-1",
            userDisplayName: displayName!,
            teamId: Guid.NewGuid(),
            teamName: "Team",
            experienceLevel: OnboardingExperienceLevel.Junior,
            checklistItems: "[]",
            totalItems: 1,
            tenantId: Guid.NewGuid(),
            startedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Rejects_Default_TeamId()
    {
        var act = () => OnboardingSession.Create(
            userId: "user-1",
            userDisplayName: "Name",
            teamId: Guid.Empty,
            teamName: "Team",
            experienceLevel: OnboardingExperienceLevel.Junior,
            checklistItems: "[]",
            totalItems: 1,
            tenantId: Guid.NewGuid(),
            startedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_TeamName(string? teamName)
    {
        var act = () => OnboardingSession.Create(
            userId: "user-1",
            userDisplayName: "Name",
            teamId: Guid.NewGuid(),
            teamName: teamName!,
            experienceLevel: OnboardingExperienceLevel.Junior,
            checklistItems: "[]",
            totalItems: 1,
            tenantId: Guid.NewGuid(),
            startedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Rejects_Invalid_ExperienceLevel()
    {
        var act = () => OnboardingSession.Create(
            userId: "user-1",
            userDisplayName: "Name",
            teamId: Guid.NewGuid(),
            teamName: "Team",
            experienceLevel: (OnboardingExperienceLevel)99,
            checklistItems: "[]",
            totalItems: 1,
            tenantId: Guid.NewGuid(),
            startedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Invalid_ChecklistItems(string? checklist)
    {
        var act = () => OnboardingSession.Create(
            userId: "user-1",
            userDisplayName: "Name",
            teamId: Guid.NewGuid(),
            teamName: "Team",
            experienceLevel: OnboardingExperienceLevel.Junior,
            checklistItems: checklist!,
            totalItems: 1,
            tenantId: Guid.NewGuid(),
            startedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Rejects_Invalid_TotalItems(int totalItems)
    {
        var act = () => OnboardingSession.Create(
            userId: "user-1",
            userDisplayName: "Name",
            teamId: Guid.NewGuid(),
            teamName: "Team",
            experienceLevel: OnboardingExperienceLevel.Junior,
            checklistItems: "[]",
            totalItems: totalItems,
            tenantId: Guid.NewGuid(),
            startedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Rejects_Default_TenantId()
    {
        var act = () => OnboardingSession.Create(
            userId: "user-1",
            userDisplayName: "Name",
            teamId: Guid.NewGuid(),
            teamName: "Team",
            experienceLevel: OnboardingExperienceLevel.Junior,
            checklistItems: "[]",
            totalItems: 1,
            tenantId: Guid.Empty,
            startedAt: DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── UpdateProgress ──────────────────────────────────────────────────

    [Fact]
    public void UpdateProgress_Recalculates_ProgressPercent()
    {
        var session = CreateValidSession(totalItems: 10);

        session.UpdateProgress(
            completedItems: 3,
            servicesExplored: "[\"svc-1\"]",
            contractsReviewed: "[\"ctr-1\"]",
            runbooksRead: null,
            aiInteractionCount: 5);

        session.CompletedItems.Should().Be(3);
        session.ProgressPercent.Should().Be(30);
        session.ServicesExplored.Should().Be("[\"svc-1\"]");
        session.ContractsReviewed.Should().Be("[\"ctr-1\"]");
        session.RunbooksRead.Should().BeNull();
        session.AiInteractionCount.Should().Be(5);
    }

    [Fact]
    public void UpdateProgress_Full_Completion()
    {
        var session = CreateValidSession(totalItems: 4);
        session.UpdateProgress(4, null, null, null, 10);

        session.ProgressPercent.Should().Be(100);
    }

    [Fact]
    public void UpdateProgress_Rejects_Negative_CompletedItems()
    {
        var session = CreateValidSession();
        var act = () => session.UpdateProgress(-1, null, null, null, 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateProgress_Rejects_Negative_AiInteractionCount()
    {
        var session = CreateValidSession();
        var act = () => session.UpdateProgress(0, null, null, null, -1);
        act.Should().Throw<ArgumentException>();
    }

    // ── Complete ────────────────────────────────────────────────────────

    [Fact]
    public void Complete_Sets_Status_And_CompletedAt()
    {
        var session = CreateValidSession();
        var completedAt = DateTimeOffset.UtcNow;

        session.Complete(completedAt);

        session.Status.Should().Be(OnboardingSessionStatus.Completed);
        session.CompletedAt.Should().Be(completedAt);
    }

    // ── Abandon ─────────────────────────────────────────────────────────

    [Fact]
    public void Abandon_Sets_Status_And_CompletedAt()
    {
        var session = CreateValidSession();
        var abandonedAt = DateTimeOffset.UtcNow;

        session.Abandon(abandonedAt);

        session.Status.Should().Be(OnboardingSessionStatus.Abandoned);
        session.CompletedAt.Should().Be(abandonedAt);
    }

    // ── Strongly-typed ID ───────────────────────────────────────────────

    [Fact]
    public void OnboardingSessionId_New_Creates_Unique_Id()
    {
        var id1 = OnboardingSessionId.New();
        var id2 = OnboardingSessionId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void OnboardingSessionId_From_Preserves_Value()
    {
        var guid = Guid.NewGuid();
        var id = OnboardingSessionId.From(guid);

        id.Value.Should().Be(guid);
    }

    // ── Helper ──────────────────────────────────────────────────────────

    private static OnboardingSession CreateValidSession(int totalItems = 5) =>
        OnboardingSession.Create(
            userId: "user-1",
            userDisplayName: "Alice Smith",
            teamId: Guid.NewGuid(),
            teamName: "Platform Team",
            experienceLevel: OnboardingExperienceLevel.Mid,
            checklistItems: "[\"item1\",\"item2\"]",
            totalItems: totalItems,
            tenantId: Guid.NewGuid(),
            startedAt: DateTimeOffset.UtcNow);
}
