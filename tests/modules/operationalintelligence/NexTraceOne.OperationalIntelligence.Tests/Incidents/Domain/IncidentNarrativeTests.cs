using FluentAssertions;

using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Domain;

/// <summary>
/// Testes unitários para a entidade IncidentNarrative.
/// Validam criação, guard clauses, MarkAsStale e Refresh.
/// </summary>
public sealed class IncidentNarrativeTests
{
    private static readonly Guid ValidIncidentId = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001");
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static IncidentNarrative CreateValid() =>
        IncidentNarrative.Create(
            IncidentNarrativeId.New(),
            ValidIncidentId,
            narrativeText: "## Incident Narrative\nSample text",
            symptomsSection: "High latency observed",
            timelineSection: "Detected at 10:00 UTC",
            probableCauseSection: "Database connection pool exhaustion",
            mitigationSection: "Pool size increased",
            relatedChangesSection: "Deploy v2.14.0",
            affectedServicesSection: "payment-gateway",
            modelUsed: "template-v1",
            tokensUsed: 150,
            status: NarrativeStatus.Draft,
            tenantId: Guid.NewGuid(),
            generatedAt: FixedNow);

    // ── Create: válido ──────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldSetAllProperties()
    {
        var narrative = CreateValid();

        narrative.IncidentId.Should().Be(ValidIncidentId);
        narrative.NarrativeText.Should().Contain("Incident Narrative");
        narrative.SymptomsSection.Should().Be("High latency observed");
        narrative.TimelineSection.Should().Be("Detected at 10:00 UTC");
        narrative.ProbableCauseSection.Should().Be("Database connection pool exhaustion");
        narrative.MitigationSection.Should().Be("Pool size increased");
        narrative.RelatedChangesSection.Should().Be("Deploy v2.14.0");
        narrative.AffectedServicesSection.Should().Be("payment-gateway");
        narrative.ModelUsed.Should().Be("template-v1");
        narrative.TokensUsed.Should().Be(150);
        narrative.Status.Should().Be(NarrativeStatus.Draft);
        narrative.GeneratedAt.Should().Be(FixedNow);
        narrative.RefreshCount.Should().Be(0);
        narrative.LastRefreshedAt.Should().BeNull();
    }

    // ── Create: guard clauses ───────────────────────────────────────────

    [Fact]
    public void Create_WithDefaultIncidentId_ShouldThrow()
    {
        var act = () => IncidentNarrative.Create(
            IncidentNarrativeId.New(),
            Guid.Empty,
            "text", null, null, null, null, null, null,
            "model", 0, NarrativeStatus.Draft, null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidNarrativeText_ShouldThrow(string? text)
    {
        var act = () => IncidentNarrative.Create(
            IncidentNarrativeId.New(),
            ValidIncidentId,
            text!, null, null, null, null, null, null,
            "model", 0, NarrativeStatus.Draft, null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidModelUsed_ShouldThrow(string? model)
    {
        var act = () => IncidentNarrative.Create(
            IncidentNarrativeId.New(),
            ValidIncidentId,
            "text", null, null, null, null, null, null,
            model!, 0, NarrativeStatus.Draft, null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNegativeTokens_ShouldThrow()
    {
        var act = () => IncidentNarrative.Create(
            IncidentNarrativeId.New(),
            ValidIncidentId,
            "text", null, null, null, null, null, null,
            "model", -1, NarrativeStatus.Draft, null, FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── MarkAsStale ─────────────────────────────────────────────────────

    [Fact]
    public void MarkAsStale_ShouldSetStatusToStale()
    {
        var narrative = CreateValid();

        narrative.MarkAsStale();

        narrative.Status.Should().Be(NarrativeStatus.Stale);
    }

    // ── Refresh ─────────────────────────────────────────────────────────

    [Fact]
    public void Refresh_ShouldUpdateTextAndIncrementCount()
    {
        var narrative = CreateValid();
        var refreshedAt = FixedNow.AddHours(2);

        narrative.Refresh(
            "Updated narrative text",
            "New symptoms",
            "New timeline",
            "New cause",
            "New mitigation",
            "New changes",
            "New services",
            "gpt-4o",
            200,
            refreshedAt);

        narrative.NarrativeText.Should().Be("Updated narrative text");
        narrative.SymptomsSection.Should().Be("New symptoms");
        narrative.TimelineSection.Should().Be("New timeline");
        narrative.ProbableCauseSection.Should().Be("New cause");
        narrative.MitigationSection.Should().Be("New mitigation");
        narrative.RelatedChangesSection.Should().Be("New changes");
        narrative.AffectedServicesSection.Should().Be("New services");
        narrative.ModelUsed.Should().Be("gpt-4o");
        narrative.TokensUsed.Should().Be(200);
        narrative.Status.Should().Be(NarrativeStatus.Draft);
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
    public void IncidentNarrativeId_New_ShouldGenerateUniqueIds()
    {
        var id1 = IncidentNarrativeId.New();
        var id2 = IncidentNarrativeId.New();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void IncidentNarrativeId_From_ShouldPreserveGuid()
    {
        var guid = Guid.NewGuid();
        var id = IncidentNarrativeId.From(guid);

        id.Value.Should().Be(guid);
    }
}
