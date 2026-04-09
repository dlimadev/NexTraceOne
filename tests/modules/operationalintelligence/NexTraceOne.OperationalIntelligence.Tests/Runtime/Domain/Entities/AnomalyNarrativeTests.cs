using FluentAssertions;

using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Domain.Entities;

/// <summary>
/// Testes unitários para a entidade AnomalyNarrative.
/// Validam criação, guard clauses, MarkAsStale e Refresh.
/// </summary>
public sealed class AnomalyNarrativeTests
{
    private static readonly DriftFindingId ValidDriftFindingId = DriftFindingId.New();
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static AnomalyNarrative CreateValid() =>
        AnomalyNarrative.Create(
            AnomalyNarrativeId.New(),
            ValidDriftFindingId,
            narrativeText: "## Anomaly Narrative\nSample text",
            symptomsSection: "High latency observed on AvgLatencyMs",
            baselineComparisonSection: "Expected: 100, Actual: 180, Deviation: 80%",
            probableCauseSection: "Recent deploy may have introduced regression",
            correlatedChangesSection: "Deploy v2.14.0",
            recommendedActionsSection: "Investigate order-service in production",
            severityJustificationSection: "Severity High based on 80% deviation",
            modelUsed: "template-v1",
            tokensUsed: 150,
            status: AnomalyNarrativeStatus.Draft,
            tenantId: Guid.NewGuid(),
            generatedAt: FixedNow);

    // ── Create: válido ──────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldSetAllProperties()
    {
        var narrative = CreateValid();

        narrative.DriftFindingId.Should().Be(ValidDriftFindingId);
        narrative.NarrativeText.Should().Contain("Anomaly Narrative");
        narrative.SymptomsSection.Should().Be("High latency observed on AvgLatencyMs");
        narrative.BaselineComparisonSection.Should().Be("Expected: 100, Actual: 180, Deviation: 80%");
        narrative.ProbableCauseSection.Should().Be("Recent deploy may have introduced regression");
        narrative.CorrelatedChangesSection.Should().Be("Deploy v2.14.0");
        narrative.RecommendedActionsSection.Should().Be("Investigate order-service in production");
        narrative.SeverityJustificationSection.Should().Be("Severity High based on 80% deviation");
        narrative.ModelUsed.Should().Be("template-v1");
        narrative.TokensUsed.Should().Be(150);
        narrative.Status.Should().Be(AnomalyNarrativeStatus.Draft);
        narrative.GeneratedAt.Should().Be(FixedNow);
        narrative.RefreshCount.Should().Be(0);
        narrative.LastRefreshedAt.Should().BeNull();
    }

    // ── Create: guard clauses ───────────────────────────────────────────

    [Fact]
    public void Create_WithNullDriftFindingId_ShouldThrow()
    {
        var act = () => AnomalyNarrative.Create(
            AnomalyNarrativeId.New(),
            null!,
            "text", null, null, null, null, null, null,
            "model", 0, AnomalyNarrativeStatus.Draft, null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidNarrativeText_ShouldThrow(string? text)
    {
        var act = () => AnomalyNarrative.Create(
            AnomalyNarrativeId.New(),
            ValidDriftFindingId,
            text!, null, null, null, null, null, null,
            "model", 0, AnomalyNarrativeStatus.Draft, null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidModelUsed_ShouldThrow(string? model)
    {
        var act = () => AnomalyNarrative.Create(
            AnomalyNarrativeId.New(),
            ValidDriftFindingId,
            "text", null, null, null, null, null, null,
            model!, 0, AnomalyNarrativeStatus.Draft, null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNegativeTokens_ShouldThrow()
    {
        var act = () => AnomalyNarrative.Create(
            AnomalyNarrativeId.New(),
            ValidDriftFindingId,
            "text", null, null, null, null, null, null,
            "model", -1, AnomalyNarrativeStatus.Draft, null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── MarkAsStale ─────────────────────────────────────────────────────

    [Fact]
    public void MarkAsStale_ShouldSetStatusToStale()
    {
        var narrative = CreateValid();

        narrative.MarkAsStale();

        narrative.Status.Should().Be(AnomalyNarrativeStatus.Stale);
    }

    // ── Refresh ─────────────────────────────────────────────────────────

    [Fact]
    public void Refresh_ShouldUpdateTextAndIncrementCount()
    {
        var narrative = CreateValid();
        var refreshedAt = FixedNow.AddHours(2);

        narrative.Refresh(
            "Updated anomaly narrative text",
            "New symptoms",
            "New baseline comparison",
            "New cause",
            "New correlated changes",
            "New recommended actions",
            "New severity justification",
            "gpt-4o",
            200,
            refreshedAt);

        narrative.NarrativeText.Should().Be("Updated anomaly narrative text");
        narrative.SymptomsSection.Should().Be("New symptoms");
        narrative.BaselineComparisonSection.Should().Be("New baseline comparison");
        narrative.ProbableCauseSection.Should().Be("New cause");
        narrative.CorrelatedChangesSection.Should().Be("New correlated changes");
        narrative.RecommendedActionsSection.Should().Be("New recommended actions");
        narrative.SeverityJustificationSection.Should().Be("New severity justification");
        narrative.ModelUsed.Should().Be("gpt-4o");
        narrative.TokensUsed.Should().Be(200);
        narrative.Status.Should().Be(AnomalyNarrativeStatus.Draft);
        narrative.RefreshCount.Should().Be(1);
        narrative.LastRefreshedAt.Should().Be(refreshedAt);
    }

    [Fact]
    public void Refresh_CalledMultipleTimes_ShouldIncrementCount()
    {
        var narrative = CreateValid();

        narrative.Refresh("v2", null, null, null, null, null, null, "model", 0, FixedNow.AddHours(1));
        narrative.Refresh("v3", null, null, null, null, null, null, "model", 0, FixedNow.AddHours(2));
        narrative.Refresh("v4", null, null, null, null, null, null, "model", 0, FixedNow.AddHours(3));

        narrative.RefreshCount.Should().Be(3);
        narrative.NarrativeText.Should().Be("v4");
    }

    [Fact]
    public void Refresh_WithInvalidNarrativeText_ShouldThrow()
    {
        var narrative = CreateValid();

        var act = () => narrative.Refresh(
            "", null, null, null, null, null, null,
            "model", 0, FixedNow.AddHours(1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Refresh_WithInvalidModel_ShouldThrow()
    {
        var narrative = CreateValid();

        var act = () => narrative.Refresh(
            "text", null, null, null, null, null, null,
            "", 0, FixedNow.AddHours(1));

        act.Should().Throw<ArgumentException>();
    }

    // ── Strongly Typed Id ───────────────────────────────────────────────

    [Fact]
    public void AnomalyNarrativeId_New_ShouldGenerateUniqueIds()
    {
        var id1 = AnomalyNarrativeId.New();
        var id2 = AnomalyNarrativeId.New();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void AnomalyNarrativeId_From_ShouldPreserveGuid()
    {
        var guid = Guid.NewGuid();
        var id = AnomalyNarrativeId.From(guid);

        id.Value.Should().Be(guid);
    }
}
