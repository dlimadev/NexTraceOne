using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.Features.GetPortalAdoptionFunnelReport;
using NexTraceOne.ProductAnalytics.Application.Features.GetSelfServiceWorkflowHealthReport;

namespace NexTraceOne.ProductAnalytics.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave BA — GetPortalAdoptionFunnelReport e GetSelfServiceWorkflowHealthReport.
/// </summary>
public sealed class WaveBaPortalIntelligenceTests
{
    private readonly IPortalAdoptionReader _adoptionReader = Substitute.For<IPortalAdoptionReader>();
    private readonly ISelfServiceWorkflowReader _workflowReader = Substitute.For<ISelfServiceWorkflowReader>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentTenant _currentTenant = Substitute.For<ICurrentTenant>();
    private static readonly DateTimeOffset Now = new(2025, 10, 1, 10, 0, 0, TimeSpan.Zero);

    public WaveBaPortalIntelligenceTests()
    {
        _clock.UtcNow.Returns(Now);
        _currentTenant.Id.Returns(Guid.NewGuid());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetPortalAdoptionFunnelReport
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPortalAdoptionFunnelReport_WithNoTeams_ReturnsEmpty()
    {
        _adoptionReader.ListTeamAdoptionAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _adoptionReader.ListInactiveUsersAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _adoptionReader.GetAdoptionTrendAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetPortalAdoptionFunnelReport.Handler(_adoptionReader, _clock, _currentTenant);
        var result = await handler.Handle(new GetPortalAdoptionFunnelReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ByTeam.Should().BeEmpty();
        result.Value.Summary.TenantAdoptionScore.Should().Be(100m);
        result.Value.EnablementOpportunityList.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPortalAdoptionFunnelReport_LeaderTeam_HighAdoptionScore()
    {
        var featureStats = new List<IPortalAdoptionReader.FeatureAdoptionStat>
        {
            new("ContractStudio", 10, 8, 7),
            new("ChangeIntelligence", 10, 9, 8),
            new("ServiceCatalog", 10, 10, 9)
        };
        var entry = new IPortalAdoptionReader.TeamAdoptionEntry("t1", "Team Alpha", 10, featureStats, Now.AddHours(-1));
        _adoptionReader.ListTeamAdoptionAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([entry]);
        _adoptionReader.ListInactiveUsersAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _adoptionReader.GetAdoptionTrendAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetPortalAdoptionFunnelReport.Handler(_adoptionReader, _clock, _currentTenant);
        var result = await handler.Handle(new GetPortalAdoptionFunnelReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var team = result.Value!.ByTeam.Single();
        team.AdoptionTier.Should().Be(GetPortalAdoptionFunnelReport.TeamAdoptionTier.Leader);
        team.OverallAdoptionScore.Should().Be(100m);
        team.FeatureGaps.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPortalAdoptionFunnelReport_InactiveTeam_LowAdoptionScore()
    {
        var featureStats = new List<IPortalAdoptionReader.FeatureAdoptionStat>
        {
            new("ContractStudio", 5, 1, 0),
            new("ChangeIntelligence", 5, 0, 0)
        };
        var entry = new IPortalAdoptionReader.TeamAdoptionEntry("t2", "Team Beta", 5, featureStats, null);
        _adoptionReader.ListTeamAdoptionAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([entry]);
        _adoptionReader.ListInactiveUsersAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _adoptionReader.GetAdoptionTrendAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetPortalAdoptionFunnelReport.Handler(_adoptionReader, _clock, _currentTenant);
        var result = await handler.Handle(new GetPortalAdoptionFunnelReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var team = result.Value!.ByTeam.Single();
        team.AdoptionTier.Should().Be(GetPortalAdoptionFunnelReport.TeamAdoptionTier.Inactive);
        team.FeatureGaps.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPortalAdoptionFunnelReport_FunnelDropRate_Computed()
    {
        var featureStats = new List<IPortalAdoptionReader.FeatureAdoptionStat>
        {
            new("ContractStudio", 10, 6, 4) // 40% drop
        };
        var entry = new IPortalAdoptionReader.TeamAdoptionEntry("t1", "Team Gamma", 10, featureStats, Now.AddMinutes(-30));
        _adoptionReader.ListTeamAdoptionAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([entry]);
        _adoptionReader.ListInactiveUsersAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _adoptionReader.GetAdoptionTrendAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetPortalAdoptionFunnelReport.Handler(_adoptionReader, _clock, _currentTenant);
        var result = await handler.Handle(new GetPortalAdoptionFunnelReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var funnelEntry = result.Value!.ByTeam.Single().FeatureFunnel.Single();
        funnelEntry.FunnelDropRate.Should().Be(40m);
    }

    [Fact]
    public async Task GetPortalAdoptionFunnelReport_InactiveUsers_Populated()
    {
        _adoptionReader.ListTeamAdoptionAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _adoptionReader.ListInactiveUsersAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([
                new IPortalAdoptionReader.InactiveUserEntry("u1", "Alice", "Team A", Now.AddDays(-35)),
                new IPortalAdoptionReader.InactiveUserEntry("u2", "Bob", "Team B", null)
            ]);
        _adoptionReader.GetAdoptionTrendAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetPortalAdoptionFunnelReport.Handler(_adoptionReader, _clock, _currentTenant);
        var result = await handler.Handle(new GetPortalAdoptionFunnelReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.InactiveUsers.Should().HaveCount(2);
        result.Value.Summary.InactiveUserCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPortalAdoptionFunnelReport_EnablementOpportunities_TopNRespected()
    {
        var makeEntry = (string teamId, string teamName) =>
        {
            var stats = Enumerable.Range(1, 5)
                .Select(i => new IPortalAdoptionReader.FeatureAdoptionStat($"Feature{i}", 5, 1, 0))
                .ToList();
            return new IPortalAdoptionReader.TeamAdoptionEntry(teamId, teamName, 5, stats, null);
        };

        _adoptionReader.ListTeamAdoptionAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([makeEntry("t1", "TeamA"), makeEntry("t2", "TeamB"), makeEntry("t3", "TeamC")]);
        _adoptionReader.ListInactiveUsersAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _adoptionReader.GetAdoptionTrendAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetPortalAdoptionFunnelReport.Handler(_adoptionReader, _clock, _currentTenant);
        var result = await handler.Handle(new GetPortalAdoptionFunnelReport.Query(TopEnablementOpportunities: 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EnablementOpportunityList.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetPortalAdoptionFunnelReport_GrowingTrend_Detected()
    {
        _adoptionReader.ListTeamAdoptionAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _adoptionReader.ListInactiveUsersAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        // Trend: 30 days ago 20% active → today 40% active = Growing
        _adoptionReader.GetAdoptionTrendAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([
                new IPortalAdoptionReader.DailyAdoptionSnapshot(90, 20, 100),
                new IPortalAdoptionReader.DailyAdoptionSnapshot(0, 40, 100)
            ]);

        var handler = new GetPortalAdoptionFunnelReport.Handler(_adoptionReader, _clock, _currentTenant);
        var result = await handler.Handle(new GetPortalAdoptionFunnelReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HistoricalAdoptionTrend.Should().Be(GetPortalAdoptionFunnelReport.AdoptionTrendDirection.Growing);
    }

    [Fact]
    public async Task GetPortalAdoptionFunnelReport_DecliningTrend_Detected()
    {
        _adoptionReader.ListTeamAdoptionAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _adoptionReader.ListInactiveUsersAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        // Trend: 90 days ago 60% active → today 30% active = Declining
        _adoptionReader.GetAdoptionTrendAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([
                new IPortalAdoptionReader.DailyAdoptionSnapshot(90, 60, 100),
                new IPortalAdoptionReader.DailyAdoptionSnapshot(0, 30, 100)
            ]);

        var handler = new GetPortalAdoptionFunnelReport.Handler(_adoptionReader, _clock, _currentTenant);
        var result = await handler.Handle(new GetPortalAdoptionFunnelReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HistoricalAdoptionTrend.Should().Be(GetPortalAdoptionFunnelReport.AdoptionTrendDirection.Declining);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetSelfServiceWorkflowHealthReport
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetSelfServiceWorkflowHealth_NoWorkflows_ReturnsEmpty()
    {
        _workflowReader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _workflowReader.GetAbandonmentHotspotsAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _workflowReader.GetTrendByReleaseAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetSelfServiceWorkflowHealthReport.Handler(_workflowReader, _clock);
        var result = await handler.Handle(new GetSelfServiceWorkflowHealthReport.Query("t1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Workflows.Should().BeEmpty();
        result.Value.Summary.OverallSelfServiceScore.Should().Be(100m);
    }

    [Fact]
    public async Task GetSelfServiceWorkflowHealth_SmoothWorkflow_ClassifiedCorrectly()
    {
        var entry = new ISelfServiceWorkflowReader.WorkflowExecutionEntry(
            "CreateService", 100, 95, 3, 2, 5.5);
        _workflowReader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([entry]);
        _workflowReader.GetAbandonmentHotspotsAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _workflowReader.GetTrendByReleaseAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetSelfServiceWorkflowHealthReport.Handler(_workflowReader, _clock);
        var result = await handler.Handle(new GetSelfServiceWorkflowHealthReport.Query("t1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Workflows.Single().HealthTier.Should().Be(GetSelfServiceWorkflowHealthReport.WorkflowHealthTier.Smooth);
        result.Value.Summary.SmoothWorkflowCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSelfServiceWorkflowHealth_BrokenWorkflow_ClassifiedCorrectly()
    {
        var entry = new ISelfServiceWorkflowReader.WorkflowExecutionEntry(
            "RequestPromotion", 100, 40, 50, 30, 120.0);
        _workflowReader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([entry]);
        _workflowReader.GetAbandonmentHotspotsAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _workflowReader.GetTrendByReleaseAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetSelfServiceWorkflowHealthReport.Handler(_workflowReader, _clock);
        var result = await handler.Handle(new GetSelfServiceWorkflowHealthReport.Query("t1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Workflows.Single().HealthTier.Should().Be(GetSelfServiceWorkflowHealthReport.WorkflowHealthTier.Broken);
        result.Value.Summary.BrokenWorkflowCount.Should().Be(1);
        result.Value.Summary.FrictionWorkflows.Should().Contain("RequestPromotion");
    }

    [Fact]
    public async Task GetSelfServiceWorkflowHealth_AdminDependencyIndex_Computed()
    {
        var entries = new[]
        {
            new ISelfServiceWorkflowReader.WorkflowExecutionEntry("CreateService", 100, 95, 3, 10, 5.0),
            new ISelfServiceWorkflowReader.WorkflowExecutionEntry("CreateContractDraft", 50, 40, 8, 5, 8.0)
        };
        _workflowReader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        _workflowReader.GetAbandonmentHotspotsAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _workflowReader.GetTrendByReleaseAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetSelfServiceWorkflowHealthReport.Handler(_workflowReader, _clock);
        var result = await handler.Handle(new GetSelfServiceWorkflowHealthReport.Query("t1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Summary.AdminDependencyIndex.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task GetSelfServiceWorkflowHealth_SlowestWorkflows_TopFive()
    {
        var entries = Enumerable.Range(1, 8)
            .Select(i => new ISelfServiceWorkflowReader.WorkflowExecutionEntry(
                $"Workflow{i}", 10, 9, 1, 1, i * 10.0))
            .ToArray();
        _workflowReader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(entries);
        _workflowReader.GetAbandonmentHotspotsAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _workflowReader.GetTrendByReleaseAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetSelfServiceWorkflowHealthReport.Handler(_workflowReader, _clock);
        var result = await handler.Handle(new GetSelfServiceWorkflowHealthReport.Query("t1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SlowestWorkflows.Should().HaveCount(5);
        result.Value.SlowestWorkflows.First().AvgCompletionTimeMinutes.Should().BeGreaterThan(
            result.Value.SlowestWorkflows.Last().AvgCompletionTimeMinutes);
    }

    [Fact]
    public async Task GetSelfServiceWorkflowHealth_Hotspots_Returned()
    {
        _workflowReader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _workflowReader.GetAbandonmentHotspotsAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([
                new ISelfServiceWorkflowReader.AbandonmentHotspot("CreateService", "Schema Validation", 25, "Users abandon at schema validation step")
            ]);
        _workflowReader.GetTrendByReleaseAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetSelfServiceWorkflowHealthReport.Handler(_workflowReader, _clock);
        var result = await handler.Handle(new GetSelfServiceWorkflowHealthReport.Query("t1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WorkflowAbandonmentHotspots.Should().HaveCount(1);
        result.Value.WorkflowAbandonmentHotspots.Single().StepName.Should().Be("Schema Validation");
    }

    [Fact]
    public async Task GetSelfServiceWorkflowHealth_ReleaseTrend_Returned()
    {
        _workflowReader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _workflowReader.GetAbandonmentHotspotsAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _workflowReader.GetTrendByReleaseAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([
                new ISelfServiceWorkflowReader.WorkflowReleaseSnapshot("v2.1.0", Now.AddDays(-10), 85.0),
                new ISelfServiceWorkflowReader.WorkflowReleaseSnapshot("v2.2.0", Now.AddDays(-2), 92.0)
            ]);

        var handler = new GetSelfServiceWorkflowHealthReport.Handler(_workflowReader, _clock);
        var result = await handler.Handle(new GetSelfServiceWorkflowHealthReport.Query("t1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WorkflowTrendByFeatureRelease.Should().HaveCount(2);
        result.Value.WorkflowTrendByFeatureRelease.Last().AvgCompletionRate.Should().Be(92m);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NullPortalAdoptionReader + NullSelfServiceWorkflowReader
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task NullPortalAdoptionReader_ReturnsEmptyCollections()
    {
        var reader = new NullPortalAdoptionReader();

        var teams = await reader.ListTeamAdoptionAsync("t1", Now.AddDays(-30), Now, CancellationToken.None);
        var inactive = await reader.ListInactiveUsersAsync("t1", Now.AddDays(-30), CancellationToken.None);
        var trend = await reader.GetAdoptionTrendAsync("t1", Now.AddDays(-90), Now, CancellationToken.None);

        teams.Should().BeEmpty();
        inactive.Should().BeEmpty();
        trend.Should().BeEmpty();
    }

    [Fact]
    public async Task NullSelfServiceWorkflowReader_ReturnsEmptyCollections()
    {
        var reader = new NullSelfServiceWorkflowReader();

        var executions = await reader.ListByTenantAsync("t1", Now.AddDays(-30), Now, CancellationToken.None);
        var hotspots = await reader.GetAbandonmentHotspotsAsync("t1", Now.AddDays(-30), Now, CancellationToken.None);
        var trend = await reader.GetTrendByReleaseAsync("t1", Now.AddDays(-30), Now, CancellationToken.None);

        executions.Should().BeEmpty();
        hotspots.Should().BeEmpty();
        trend.Should().BeEmpty();
    }
}
