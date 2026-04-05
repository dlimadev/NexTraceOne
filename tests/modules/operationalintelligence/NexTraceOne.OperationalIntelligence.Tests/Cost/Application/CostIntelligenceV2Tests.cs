using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.CorrelateCloudCostWithChange;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.DetectCostAnomalies;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.ForecastBudget;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GenerateEfficiencyRecommendations;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetBudgetForecast;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetShowbackReport;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.ListEfficiencyRecommendations;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;
using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Cost.Application;

/// <summary>
/// Testes unitários para os handlers introduzidos na P4.4 — Cost Intelligence V2:
/// ForecastBudget, GetBudgetForecast, GenerateEfficiencyRecommendations,
/// ListEfficiencyRecommendations, GetShowbackReport,
/// CorrelateCloudCostWithChange e DetectCostAnomalies.
/// </summary>
public sealed class CostIntelligenceV2Tests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 5, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid BatchId = Guid.NewGuid();
    private static readonly Guid ReleaseId = Guid.NewGuid();

    private static IDateTimeProvider MockClock()
    {
        var c = Substitute.For<IDateTimeProvider>();
        c.UtcNow.Returns(FixedNow);
        return c;
    }

    private static CostRecord MakeRecord(
        string serviceId,
        string serviceName,
        string? team = "team-a",
        string? domain = "commerce",
        string? env = "production",
        decimal cost = 100m) =>
        CostRecord.Create(BatchId, serviceId, serviceName, team, domain, env, "2026-04", cost, "USD", "AWS CUR", FixedNow).Value;

    // ── BudgetForecast.Create — domain tests ─────────────────────────────────

    [Fact]
    public void BudgetForecast_Create_WithValidData_ShouldSucceed()
    {
        var result = BudgetForecast.Create(
            "svc-api", "API Service", "production", "2026-04",
            1200m, 1500m, 85m, "LinearTrend", null, FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceId.Should().Be("svc-api");
        result.Value.ProjectedCost.Should().Be(1200m);
        result.Value.BudgetLimit.Should().Be(1500m);
        result.Value.IsOverBudgetProjected.Should().BeFalse();
        result.Value.ConfidencePercent.Should().Be(85m);
    }

    [Fact]
    public void BudgetForecast_Create_WhenProjectedExceedsBudget_ShouldMarkOverBudget()
    {
        var result = BudgetForecast.Create(
            "svc-api", "API Service", "production", "2026-04",
            2000m, 1500m, 70m, "LinearTrend", null, FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOverBudgetProjected.Should().BeTrue();
    }

    [Fact]
    public void BudgetForecast_Create_WithNegativeCost_ShouldFail()
    {
        var result = BudgetForecast.Create(
            "svc-api", "API Service", "production", "2026-04",
            -100m, null, 50m, "LinearTrend", null, FixedNow);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void BudgetForecast_Create_WithInvalidConfidence_ShouldFail()
    {
        var result = BudgetForecast.Create(
            "svc-api", "API Service", "production", "2026-04",
            1000m, null, 150m, "LinearTrend", null, FixedNow);

        result.IsFailure.Should().BeTrue();
    }

    // ── EfficiencyRecommendation.Create — domain tests ───────────────────────

    [Fact]
    public void EfficiencyRecommendation_Create_WithHighDeviation_ShouldSetHighPriority()
    {
        var result = EfficiencyRecommendation.Create(
            "svc-api", "API Service", "production",
            500m, 200m,
            "Service costs significantly more than peers.",
            "Compute", FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.Priority.Should().Be("High");
        result.Value.DeviationPercent.Should().BeGreaterThan(100m);
        result.Value.IsAcknowledged.Should().BeFalse();
    }

    [Fact]
    public void EfficiencyRecommendation_Create_WithMediumDeviation_ShouldSetMediumPriority()
    {
        var result = EfficiencyRecommendation.Create(
            "svc-api", "API Service", "production",
            300m, 200m,
            "Service costs moderately more than peers.",
            "Compute", FixedNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.Priority.Should().Be("Medium");
    }

    [Fact]
    public void EfficiencyRecommendation_Acknowledge_ShouldSetIsAcknowledgedTrue()
    {
        var rec = EfficiencyRecommendation.Create(
            "svc-api", "API Service", "production",
            300m, 200m,
            "Service costs more than peers.",
            "Compute", FixedNow).Value;

        rec.Acknowledge();

        rec.IsAcknowledged.Should().BeTrue();
    }

    [Fact]
    public void EfficiencyRecommendation_Create_WithEmptyText_ShouldFail()
    {
        var result = EfficiencyRecommendation.Create(
            "svc-api", "API Service", "production",
            300m, 200m,
            string.Empty,
            "Compute", FixedNow);

        result.IsFailure.Should().BeTrue();
    }

    // ── ForecastBudget handler ────────────────────────────────────────────────

    [Fact]
    public async Task ForecastBudget_WithEnoughSnapshots_ShouldUseLinearTrendMethod()
    {
        var snapshot1 = CostSnapshot.Create("svc-api", "production", 1000m, 300m, 200m, 150m, 100m,
            FixedNow.AddDays(-10), "AWS CUR", "2026-03", "USD").Value;
        var snapshot2 = CostSnapshot.Create("svc-api", "production", 1100m, 330m, 220m, 165m, 110m,
            FixedNow.AddDays(-5), "AWS CUR", "2026-04", "USD").Value;

        var snapshotRepo = Substitute.For<ICostSnapshotRepository>();
        snapshotRepo.ListByServiceAsync("svc-api", "production", 1, 6, Arg.Any<CancellationToken>())
            .Returns(new List<CostSnapshot> { snapshot1, snapshot2 });

        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        profileRepo.GetByServiceAndEnvironmentAsync("svc-api", "production", Arg.Any<CancellationToken>())
            .Returns((ServiceCostProfile?)null);

        var forecastRepo = Substitute.For<IBudgetForecastRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ForecastBudget.Handler(forecastRepo, profileRepo, snapshotRepo, uow, MockClock());
        var command = new ForecastBudget.Command("svc-api", "API Service", "production", "2026-04");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Method.Should().Be("LinearTrend");
        result.Value.ServiceId.Should().Be("svc-api");
        forecastRepo.Received(1).Add(Arg.Any<BudgetForecast>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ForecastBudget_WithInsufficientSnapshots_ShouldUseDefaultMethod()
    {
        var snapshotRepo = Substitute.For<ICostSnapshotRepository>();
        snapshotRepo.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), 1, 6, Arg.Any<CancellationToken>())
            .Returns(new List<CostSnapshot>());

        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        profileRepo.GetByServiceAndEnvironmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ServiceCostProfile?)null);

        var forecastRepo = Substitute.For<IBudgetForecastRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ForecastBudget.Handler(forecastRepo, profileRepo, snapshotRepo, uow, MockClock());
        var result = await handler.Handle(new ForecastBudget.Command("svc-x", "Service X", "staging", "2026-04"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Method.Should().Be("Insufficient data");
        result.Value.ProjectedCost.Should().Be(0m);
    }

    [Fact]
    public async Task ForecastBudget_WithBudgetProfile_ShouldSetBudgetLimitAndDetectOverBudget()
    {
        var snapshot1 = CostSnapshot.Create("svc-api", "production", 2000m, 600m, 400m, 300m, 200m,
            FixedNow.AddDays(-10), "AWS CUR", "2026-03", "USD").Value;
        var snapshot2 = CostSnapshot.Create("svc-api", "production", 2200m, 660m, 440m, 330m, 220m,
            FixedNow.AddDays(-5), "AWS CUR", "2026-04", "USD").Value;

        var snapshotRepo = Substitute.For<ICostSnapshotRepository>();
        snapshotRepo.ListByServiceAsync("svc-api", "production", 1, 6, Arg.Any<CancellationToken>())
            .Returns(new List<CostSnapshot> { snapshot1, snapshot2 });

        var profile = ServiceCostProfile.Create("svc-api", "production", 80m, FixedNow, 500m);

        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        profileRepo.GetByServiceAndEnvironmentAsync("svc-api", "production", Arg.Any<CancellationToken>())
            .Returns(profile);

        var forecastRepo = Substitute.For<IBudgetForecastRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ForecastBudget.Handler(forecastRepo, profileRepo, snapshotRepo, uow, MockClock());
        var result = await handler.Handle(new ForecastBudget.Command("svc-api", "API Service", "production", "2026-04"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BudgetLimit.Should().Be(500m);
        result.Value.IsOverBudgetProjected.Should().BeTrue();
    }

    // ── GetBudgetForecast handler ─────────────────────────────────────────────

    [Fact]
    public async Task GetBudgetForecast_WhenForecastExists_ShouldReturnResponse()
    {
        var forecast = BudgetForecast.Create(
            "svc-api", "API Service", "production", "2026-04",
            1200m, 1500m, 85m, "LinearTrend", null, FixedNow).Value;

        var repo = Substitute.For<IBudgetForecastRepository>();
        repo.GetLatestByServiceAsync("svc-api", "production", Arg.Any<CancellationToken>())
            .Returns(forecast);

        var handler = new GetBudgetForecast.Handler(repo);
        var result = await handler.Handle(new GetBudgetForecast.Query("svc-api", "production"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceId.Should().Be("svc-api");
        result.Value.Method.Should().Be("LinearTrend");
        result.Value.IsOverBudgetProjected.Should().BeFalse();
    }

    [Fact]
    public async Task GetBudgetForecast_WhenForecastNotFound_ShouldReturnFailure()
    {
        var repo = Substitute.For<IBudgetForecastRepository>();
        repo.GetLatestByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((BudgetForecast?)null);

        var handler = new GetBudgetForecast.Handler(repo);
        var result = await handler.Handle(new GetBudgetForecast.Query("svc-missing", "production"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FORECAST_NOT_FOUND");
    }

    // ── GenerateEfficiencyRecommendations handler ─────────────────────────────

    [Fact]
    public async Task GenerateEfficiencyRecommendations_WithOutlierService_ShouldCreateRecommendation()
    {
        var records = new List<CostRecord>
        {
            MakeRecord("svc-a", "Service A", cost: 100m),
            MakeRecord("svc-b", "Service B", cost: 110m),
            MakeRecord("svc-c", "Service C", cost: 105m),
            MakeRecord("svc-d", "Service D", cost: 500m), // outlier
        };

        var recordRepo = Substitute.For<ICostRecordRepository>();
        recordRepo.ListByPeriodAsync("2026-04", Arg.Any<CancellationToken>())
            .Returns(records);

        var recRepo = Substitute.For<IEfficiencyRecommendationRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new GenerateEfficiencyRecommendations.Handler(recordRepo, recRepo, uow, MockClock());
        var command = new GenerateEfficiencyRecommendations.Command(null, null, "production", "2026-04");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAnalyzed.Should().Be(4);
        result.Value.Recommendations.Should().NotBeEmpty();
        result.Value.Recommendations.Should().Contain(r => r.ServiceId == "svc-d");
        recRepo.Received(1).AddRange(Arg.Any<IEnumerable<EfficiencyRecommendation>>());
    }

    [Fact]
    public async Task GenerateEfficiencyRecommendations_WithNoRecords_ShouldReturnEmptyResponse()
    {
        var recordRepo = Substitute.For<ICostRecordRepository>();
        recordRepo.ListByPeriodAsync("2026-04", Arg.Any<CancellationToken>())
            .Returns(new List<CostRecord>());

        var recRepo = Substitute.For<IEfficiencyRecommendationRepository>();
        var uow = Substitute.For<IUnitOfWork>();

        var handler = new GenerateEfficiencyRecommendations.Handler(recordRepo, recRepo, uow, MockClock());
        var result = await handler.Handle(
            new GenerateEfficiencyRecommendations.Command(null, null, "production", "2026-04"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAnalyzed.Should().Be(0);
        result.Value.Recommendations.Should().BeEmpty();
        recRepo.DidNotReceive().AddRange(Arg.Any<IEnumerable<EfficiencyRecommendation>>());
    }

    [Fact]
    public async Task GenerateEfficiencyRecommendations_FilteredByTeam_ShouldUseTeamRecords()
    {
        var records = new List<CostRecord>
        {
            MakeRecord("svc-a", "Service A", team: "team-x", cost: 100m),
            MakeRecord("svc-b", "Service B", team: "team-x", cost: 800m),
        };

        var recordRepo = Substitute.For<ICostRecordRepository>();
        recordRepo.ListByTeamAsync("team-x", "2026-04", Arg.Any<CancellationToken>())
            .Returns(records);

        var recRepo = Substitute.For<IEfficiencyRecommendationRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new GenerateEfficiencyRecommendations.Handler(recordRepo, recRepo, uow, MockClock());
        var result = await handler.Handle(
            new GenerateEfficiencyRecommendations.Command("team-x", null, "production", "2026-04"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Recommendations.Should().NotBeEmpty();
    }

    // ── ListEfficiencyRecommendations handler ─────────────────────────────────

    [Fact]
    public async Task ListEfficiencyRecommendations_Unacknowledged_ShouldReturnItems()
    {
        var rec = EfficiencyRecommendation.Create(
            "svc-a", "Service A", "production", 500m, 200m,
            "Service A costs significantly more.", "Compute", FixedNow).Value;

        var repo = Substitute.For<IEfficiencyRecommendationRepository>();
        repo.ListUnacknowledgedAsync(Arg.Any<CancellationToken>())
            .Returns(new List<EfficiencyRecommendation> { rec });

        var handler = new ListEfficiencyRecommendations.Handler(repo);
        var result = await handler.Handle(
            new ListEfficiencyRecommendations.Query(null, null, UnacknowledgedOnly: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].ServiceId.Should().Be("svc-a");
    }

    [Fact]
    public async Task ListEfficiencyRecommendations_ByServiceAndEnv_ShouldCallListByService()
    {
        var repo = Substitute.For<IEfficiencyRecommendationRepository>();
        repo.ListByServiceAsync("svc-b", "production", Arg.Any<CancellationToken>())
            .Returns(new List<EfficiencyRecommendation>());

        var handler = new ListEfficiencyRecommendations.Handler(repo);
        var result = await handler.Handle(
            new ListEfficiencyRecommendations.Query("svc-b", "production"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).ListByServiceAsync("svc-b", "production", Arg.Any<CancellationToken>());
    }

    // ── GetShowbackReport handler ─────────────────────────────────────────────

    [Fact]
    public async Task GetShowbackReport_ByPeriod_ShouldReturnAggregatedTotals()
    {
        var records = new List<CostRecord>
        {
            MakeRecord("svc-a", "Service A", team: "team-1", domain: "commerce", cost: 200m),
            MakeRecord("svc-b", "Service B", team: "team-2", domain: "payments", cost: 300m),
            MakeRecord("svc-a", "Service A", team: "team-1", domain: "commerce", cost: 150m),
        };

        var recordRepo = Substitute.For<ICostRecordRepository>();
        recordRepo.ListByPeriodAsync("2026-04", Arg.Any<CancellationToken>())
            .Returns(records);

        var handler = new GetShowbackReport.Handler(recordRepo);
        var result = await handler.Handle(
            new GetShowbackReport.Query(null, null, null, "2026-04"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCost.Should().Be(650m);
        result.Value.ByTeam.Should().HaveCount(2);
        result.Value.ByDomain.Should().HaveCount(2);
        result.Value.ByService.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetShowbackReport_ByTeam_ShouldUseTeamFilter()
    {
        var records = new List<CostRecord>
        {
            MakeRecord("svc-a", "Service A", team: "team-1", cost: 100m),
        };

        var recordRepo = Substitute.For<ICostRecordRepository>();
        recordRepo.ListByTeamAsync("team-1", "2026-04", Arg.Any<CancellationToken>())
            .Returns(records);

        var handler = new GetShowbackReport.Handler(recordRepo);
        var result = await handler.Handle(
            new GetShowbackReport.Query("team-1", null, null, "2026-04"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCost.Should().Be(100m);
    }

    // ── CorrelateCloudCostWithChange handler ──────────────────────────────────

    [Fact]
    public async Task CorrelateCloudCostWithChange_WithMatchingRecords_ShouldComputeAttribution()
    {
        var serviceRecords = new List<CostRecord>
        {
            MakeRecord("svc-api", "API Service", cost: 1000m),
            MakeRecord("svc-api", "API Service", cost: 500m),
        };

        var releaseRecord = MakeRecord("svc-api", "API Service", cost: 300m);
        releaseRecord.AssignRelease(ReleaseId);
        var releaseRecords = new List<CostRecord> { releaseRecord };

        var recordRepo = Substitute.For<ICostRecordRepository>();
        recordRepo.ListByServiceAsync("svc-api", "2026-04", Arg.Any<CancellationToken>())
            .Returns(serviceRecords);
        recordRepo.ListByReleaseAsync(ReleaseId, Arg.Any<CancellationToken>())
            .Returns(releaseRecords);

        var handler = new CorrelateCloudCostWithChange.Handler(recordRepo);
        var result = await handler.Handle(
            new CorrelateCloudCostWithChange.Query(ReleaseId, "svc-api", "production", "2026-04"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCostForService.Should().Be(1500m);
        result.Value.CostAttributedToChange.Should().Be(300m);
        result.Value.CostAttributionPercent.Should().BeApproximately(20m, 0.001m);
        result.Value.CorrelatedRecordCount.Should().Be(1);
    }

    [Fact]
    public async Task CorrelateCloudCostWithChange_WithNoReleaseRecords_ShouldReturnZeroAttribution()
    {
        var serviceRecords = new List<CostRecord>
        {
            MakeRecord("svc-api", "API Service", cost: 1000m),
        };

        var recordRepo = Substitute.For<ICostRecordRepository>();
        recordRepo.ListByServiceAsync("svc-api", "2026-04", Arg.Any<CancellationToken>())
            .Returns(serviceRecords);
        recordRepo.ListByReleaseAsync(ReleaseId, Arg.Any<CancellationToken>())
            .Returns(new List<CostRecord>());

        var handler = new CorrelateCloudCostWithChange.Handler(recordRepo);
        var result = await handler.Handle(
            new CorrelateCloudCostWithChange.Query(ReleaseId, "svc-api", "production", "2026-04"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CostAttributedToChange.Should().Be(0m);
        result.Value.CostAttributionPercent.Should().Be(0m);
    }

    // ── DetectCostAnomalies handler ───────────────────────────────────────────

    [Fact]
    public async Task DetectCostAnomalies_WhenServiceExceedsThreshold_ShouldReturnAnomaly()
    {
        var records = new List<CostRecord>
        {
            MakeRecord("svc-api", "API Service", cost: 1200m),
        };

        var profile = ServiceCostProfile.Create("svc-api", "production", 80m, FixedNow, 1000m);

        var recordRepo = Substitute.For<ICostRecordRepository>();
        recordRepo.ListByPeriodAsync("2026-04", Arg.Any<CancellationToken>())
            .Returns(records);

        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        profileRepo.GetByServiceAndEnvironmentAsync("svc-api", "production", Arg.Any<CancellationToken>())
            .Returns(profile);

        var handler = new DetectCostAnomalies.Handler(recordRepo, profileRepo, MockClock());
        var result = await handler.Handle(
            new DetectCostAnomalies.Query("production", "2026-04"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Anomalies.Should().HaveCount(1);
        result.Value.Anomalies[0].ServiceId.Should().Be("svc-api");
        result.Value.Anomalies[0].ActualCost.Should().Be(1200m);
        result.Value.TotalServicesAnalyzed.Should().Be(1);
    }

    [Fact]
    public async Task DetectCostAnomalies_WhenServiceWithinBudget_ShouldReturnNoAnomalies()
    {
        var records = new List<CostRecord>
        {
            MakeRecord("svc-api", "API Service", cost: 500m),
        };

        var profile = ServiceCostProfile.Create("svc-api", "production", 80m, FixedNow, 1000m);

        var recordRepo = Substitute.For<ICostRecordRepository>();
        recordRepo.ListByPeriodAsync("2026-04", Arg.Any<CancellationToken>())
            .Returns(records);

        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        profileRepo.GetByServiceAndEnvironmentAsync("svc-api", "production", Arg.Any<CancellationToken>())
            .Returns(profile);

        var handler = new DetectCostAnomalies.Handler(recordRepo, profileRepo, MockClock());
        var result = await handler.Handle(
            new DetectCostAnomalies.Query("production", "2026-04"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Anomalies.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectCostAnomalies_WhenNoProfileFound_ShouldSkipService()
    {
        var records = new List<CostRecord>
        {
            MakeRecord("svc-no-profile", "No Profile Service", cost: 9999m),
        };

        var recordRepo = Substitute.For<ICostRecordRepository>();
        recordRepo.ListByPeriodAsync("2026-04", Arg.Any<CancellationToken>())
            .Returns(records);

        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        profileRepo.GetByServiceAndEnvironmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ServiceCostProfile?)null);

        var handler = new DetectCostAnomalies.Handler(recordRepo, profileRepo, MockClock());
        var result = await handler.Handle(
            new DetectCostAnomalies.Query("production", "2026-04"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Anomalies.Should().BeEmpty();
    }
}
