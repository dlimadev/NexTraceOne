using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Domain;

/// <summary>
/// Testes unitários para a entidade ExecutiveBriefing.
/// Valida factory method Generate, transições de estado (Publish/Archive) e guard clauses.
/// </summary>
public sealed class ExecutiveBriefingTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodStart = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodEnd = new(2026, 6, 15, 0, 0, 0, TimeSpan.Zero);

    // ── Factory method: Generate ──

    [Fact]
    public void Generate_ValidParameters_ShouldCreateDraftBriefing()
    {
        var briefing = CreateBriefing();

        briefing.Should().NotBeNull();
        briefing.Id.Value.Should().NotBeEmpty();
        briefing.Title.Should().Be("Weekly Platform Briefing");
        briefing.Frequency.Should().Be(BriefingFrequency.Weekly);
        briefing.Status.Should().Be(BriefingStatus.Draft);
        briefing.PeriodStart.Should().Be(PeriodStart);
        briefing.PeriodEnd.Should().Be(PeriodEnd);
        briefing.GeneratedAt.Should().Be(FixedNow);
        briefing.GeneratedByAgent.Should().Be("executive-briefing-agent");
        briefing.PublishedAt.Should().BeNull();
        briefing.ArchivedAt.Should().BeNull();
        briefing.TenantId.Should().Be("tenant1");
    }

    [Fact]
    public void Generate_WithAllSections_ShouldPopulateAllSections()
    {
        var briefing = CreateBriefing();

        briefing.PlatformStatusSection.Should().Be("{\"status\":\"healthy\"}");
        briefing.TopIncidentsSection.Should().Be("{\"incidents\":[]}");
        briefing.TeamPerformanceSection.Should().Be("{\"teams\":[]}");
        briefing.HighRiskChangesSection.Should().Be("{\"changes\":[]}");
        briefing.ComplianceStatusSection.Should().Be("{\"compliance\":\"ok\"}");
        briefing.CostTrendsSection.Should().Be("{\"trend\":\"stable\"}");
        briefing.ActiveRisksSection.Should().Be("{\"risks\":[]}");
    }

    [Fact]
    public void Generate_WithNullSections_ShouldAllowNullSections()
    {
        var briefing = ExecutiveBriefing.Generate(
            title: "Minimal Briefing",
            frequency: BriefingFrequency.OnDemand,
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            executiveSummary: null,
            platformStatusSection: null,
            topIncidentsSection: null,
            teamPerformanceSection: null,
            highRiskChangesSection: null,
            complianceStatusSection: null,
            costTrendsSection: null,
            activeRisksSection: null,
            generatedByAgent: "test-agent",
            tenantId: null,
            now: FixedNow);

        briefing.Status.Should().Be(BriefingStatus.Draft);
        briefing.PlatformStatusSection.Should().BeNull();
        briefing.TopIncidentsSection.Should().BeNull();
        briefing.TeamPerformanceSection.Should().BeNull();
        briefing.HighRiskChangesSection.Should().BeNull();
        briefing.ComplianceStatusSection.Should().BeNull();
        briefing.CostTrendsSection.Should().BeNull();
        briefing.ActiveRisksSection.Should().BeNull();
        briefing.TenantId.Should().BeNull();
    }

    [Fact]
    public void Generate_TitleTrimmed_ShouldTrimWhitespace()
    {
        var briefing = ExecutiveBriefing.Generate(
            title: "  Trimmed Title  ",
            frequency: BriefingFrequency.Daily,
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            executiveSummary: "  summary  ",
            platformStatusSection: null,
            topIncidentsSection: null,
            teamPerformanceSection: null,
            highRiskChangesSection: null,
            complianceStatusSection: null,
            costTrendsSection: null,
            activeRisksSection: null,
            generatedByAgent: "  agent  ",
            tenantId: "  t1  ",
            now: FixedNow);

        briefing.Title.Should().Be("Trimmed Title");
        briefing.ExecutiveSummary.Should().Be("summary");
        briefing.GeneratedByAgent.Should().Be("agent");
        briefing.TenantId.Should().Be("t1");
    }

    [Fact]
    public void Generate_AllFrequencies_ShouldBeAccepted()
    {
        foreach (var freq in Enum.GetValues<BriefingFrequency>())
        {
            var briefing = ExecutiveBriefing.Generate(
                title: $"Briefing {freq}",
                frequency: freq,
                periodStart: PeriodStart,
                periodEnd: PeriodEnd,
                executiveSummary: null,
                platformStatusSection: null,
                topIncidentsSection: null,
                teamPerformanceSection: null,
                highRiskChangesSection: null,
                complianceStatusSection: null,
                costTrendsSection: null,
                activeRisksSection: null,
                generatedByAgent: "test-agent",
                tenantId: null,
                now: FixedNow);

            briefing.Frequency.Should().Be(freq);
            briefing.Status.Should().Be(BriefingStatus.Draft);
        }
    }

    // ── Guard clauses ──

    [Fact]
    public void Generate_EmptyTitle_ShouldThrow()
    {
        var act = () => ExecutiveBriefing.Generate(
            title: "",
            frequency: BriefingFrequency.Weekly,
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            executiveSummary: null,
            platformStatusSection: null,
            topIncidentsSection: null,
            teamPerformanceSection: null,
            highRiskChangesSection: null,
            complianceStatusSection: null,
            costTrendsSection: null,
            activeRisksSection: null,
            generatedByAgent: "agent",
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_TitleTooLong_ShouldThrow()
    {
        var act = () => ExecutiveBriefing.Generate(
            title: new string('A', 301),
            frequency: BriefingFrequency.Weekly,
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            executiveSummary: null,
            platformStatusSection: null,
            topIncidentsSection: null,
            teamPerformanceSection: null,
            highRiskChangesSection: null,
            complianceStatusSection: null,
            costTrendsSection: null,
            activeRisksSection: null,
            generatedByAgent: "agent",
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_EmptyAgent_ShouldThrow()
    {
        var act = () => ExecutiveBriefing.Generate(
            title: "Valid Title",
            frequency: BriefingFrequency.Weekly,
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            executiveSummary: null,
            platformStatusSection: null,
            topIncidentsSection: null,
            teamPerformanceSection: null,
            highRiskChangesSection: null,
            complianceStatusSection: null,
            costTrendsSection: null,
            activeRisksSection: null,
            generatedByAgent: "",
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_AgentTooLong_ShouldThrow()
    {
        var act = () => ExecutiveBriefing.Generate(
            title: "Valid Title",
            frequency: BriefingFrequency.Weekly,
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            executiveSummary: null,
            platformStatusSection: null,
            topIncidentsSection: null,
            teamPerformanceSection: null,
            highRiskChangesSection: null,
            complianceStatusSection: null,
            costTrendsSection: null,
            activeRisksSection: null,
            generatedByAgent: new string('A', 201),
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Publish ──

    [Fact]
    public void Publish_FromDraft_ShouldTransitionToPublished()
    {
        var briefing = CreateBriefing();
        var publishTime = FixedNow.AddHours(1);

        var result = briefing.Publish(publishTime);

        result.IsSuccess.Should().BeTrue();
        briefing.Status.Should().Be(BriefingStatus.Published);
        briefing.PublishedAt.Should().Be(publishTime);
    }

    [Fact]
    public void Publish_FromPublished_ShouldReturnError()
    {
        var briefing = CreateBriefing();
        briefing.Publish(FixedNow.AddHours(1));

        var result = briefing.Publish(FixedNow.AddHours(2));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("InvalidTransition");
        briefing.Status.Should().Be(BriefingStatus.Published);
    }

    [Fact]
    public void Publish_FromArchived_ShouldReturnError()
    {
        var briefing = CreateBriefing();
        briefing.Publish(FixedNow.AddHours(1));
        briefing.Archive(FixedNow.AddHours(2));

        var result = briefing.Publish(FixedNow.AddHours(3));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("InvalidTransition");
        briefing.Status.Should().Be(BriefingStatus.Archived);
    }

    // ── Archive ──

    [Fact]
    public void Archive_FromPublished_ShouldTransitionToArchived()
    {
        var briefing = CreateBriefing();
        briefing.Publish(FixedNow.AddHours(1));
        var archiveTime = FixedNow.AddHours(2);

        var result = briefing.Archive(archiveTime);

        result.IsSuccess.Should().BeTrue();
        briefing.Status.Should().Be(BriefingStatus.Archived);
        briefing.ArchivedAt.Should().Be(archiveTime);
    }

    [Fact]
    public void Archive_FromDraft_ShouldReturnError()
    {
        var briefing = CreateBriefing();

        var result = briefing.Archive(FixedNow.AddHours(1));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("InvalidTransition");
        briefing.Status.Should().Be(BriefingStatus.Draft);
    }

    [Fact]
    public void Archive_FromArchived_ShouldReturnError()
    {
        var briefing = CreateBriefing();
        briefing.Publish(FixedNow.AddHours(1));
        briefing.Archive(FixedNow.AddHours(2));

        var result = briefing.Archive(FixedNow.AddHours(3));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("InvalidTransition");
        briefing.Status.Should().Be(BriefingStatus.Archived);
    }

    // ── Helper ──

    private static ExecutiveBriefing CreateBriefing() => ExecutiveBriefing.Generate(
        title: "Weekly Platform Briefing",
        frequency: BriefingFrequency.Weekly,
        periodStart: PeriodStart,
        periodEnd: PeriodEnd,
        executiveSummary: "All systems operational.",
        platformStatusSection: "{\"status\":\"healthy\"}",
        topIncidentsSection: "{\"incidents\":[]}",
        teamPerformanceSection: "{\"teams\":[]}",
        highRiskChangesSection: "{\"changes\":[]}",
        complianceStatusSection: "{\"compliance\":\"ok\"}",
        costTrendsSection: "{\"trend\":\"stable\"}",
        activeRisksSection: "{\"risks\":[]}",
        generatedByAgent: "executive-briefing-agent",
        tenantId: "tenant1",
        now: FixedNow);
}
