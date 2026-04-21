using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetSloServiceRankingReport;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

using SloHealthTier = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetSloServiceRankingReport.GetSloServiceRankingReport.SloHealthTier;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave N.1 — GetSloServiceRankingReport.
/// Cobre ranking de conformidade SLO por serviço, classificação de saúde,
/// filtro por ambiente e comportamento com dados vazios.
/// </summary>
public sealed class SloServiceRankingReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-slo-rank";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    /// <summary>Cria uma SloObservation com os valores dados. observedValue >= sloTarget → Met.</summary>
    private static SloObservation MakeObs(
        string service,
        string env,
        string metric,
        decimal observed,
        decimal target)
        => SloObservation.Create(
            tenantId: TenantId,
            serviceName: service,
            environment: env,
            metricName: metric,
            observedValue: observed,
            sloTarget: target,
            periodStart: FixedNow.AddHours(-2),
            periodEnd: FixedNow.AddHours(-1),
            observedAt: FixedNow.AddHours(-1));

    // ── Empty report ──────────────────────────────────────────────────────

    [Fact]
    public async Task Report_Empty_When_No_Observations()
    {
        var repo = Substitute.For<ISloObservationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<SloObservationStatus?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetSloServiceRankingReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetSloServiceRankingReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServices.Should().Be(0);
        result.Value.TotalObservations.Should().Be(0);
        result.Value.TotalBreaches.Should().Be(0);
        result.Value.ServiceRanking.Should().BeEmpty();
        result.Value.TenantAvgComplianceRate.Should().Be(0m);
    }

    // ── Single service, all Met → Excellent ──────────────────────────────

    [Fact]
    public async Task Service_AllMet_IsExcellent()
    {
        var obs = new[]
        {
            MakeObs("api-a", "prod", "latency", 99.9m, 99.0m),   // Met
            MakeObs("api-a", "prod", "availability", 100m, 99.0m) // Met
        };
        var repo = Substitute.For<ISloObservationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<SloObservationStatus?>(), Arg.Any<CancellationToken>()).Returns(obs);

        var handler = new GetSloServiceRankingReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetSloServiceRankingReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServices.Should().Be(1);
        result.Value.ServicesExcellent.Should().Be(1);
        result.Value.ServicesGood.Should().Be(0);
        result.Value.ServicesStruggling.Should().Be(0);
        var svc = result.Value.ServiceRanking[0];
        svc.HealthTier.Should().Be(SloHealthTier.Excellent);
        svc.ComplianceRatePercent.Should().Be(100m);
        svc.BreachedCount.Should().Be(0);
    }

    // ── Service below GoodThreshold → Struggling ─────────────────────────

    [Fact]
    public async Task Service_LowCompliance_IsStruggling()
    {
        // observedValue << target → Breached
        var obs = new[]
        {
            MakeObs("api-b", "prod", "latency", 50m, 99.0m),
            MakeObs("api-b", "prod", "availability", 60m, 99.0m),
            MakeObs("api-b", "prod", "error_rate", 99.5m, 99.0m)  // Met
        };
        var repo = Substitute.For<ISloObservationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<SloObservationStatus?>(), Arg.Any<CancellationToken>()).Returns(obs);

        var handler = new GetSloServiceRankingReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetSloServiceRankingReport.Query(TenantId, GoodThreshold: 80m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var svc = result.Value.ServiceRanking[0];
        svc.HealthTier.Should().Be(SloHealthTier.Struggling);
        svc.BreachedCount.Should().Be(2);
        svc.ComplianceRatePercent.Should().Be(Math.Round(1m / 3m * 100m, 2));
    }

    // ── Multiple services ranked desc by compliance ───────────────────────

    [Fact]
    public async Task ServiceRanking_OrderedByComplianceDesc()
    {
        // api-a: 100% Met; api-b: 50% Met; api-c: 0% Met
        var obs = new List<SloObservation>
        {
            MakeObs("api-a", "prod", "m1", 100m, 99m),
            MakeObs("api-b", "prod", "m1", 100m, 99m),  // Met
            MakeObs("api-b", "prod", "m2", 50m, 99m),   // Breached
            MakeObs("api-c", "prod", "m1", 10m, 99m),   // Breached
        };
        var repo = Substitute.For<ISloObservationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<SloObservationStatus?>(), Arg.Any<CancellationToken>()).Returns(obs);

        var handler = new GetSloServiceRankingReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetSloServiceRankingReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceRanking.Count.Should().Be(3);
        result.Value.ServiceRanking[0].ServiceName.Should().Be("api-a");
        result.Value.ServiceRanking[0].ComplianceRatePercent.Should().Be(100m);
        result.Value.ServiceRanking[2].ServiceName.Should().Be("api-c");
        result.Value.ServiceRanking[2].ComplianceRatePercent.Should().Be(0m);
    }

    // ── Environment filter ────────────────────────────────────────────────

    [Fact]
    public async Task EnvironmentFilter_ExcludesOtherEnvs()
    {
        var obs = new[]
        {
            MakeObs("svc-a", "prod", "m1", 100m, 99m),
            MakeObs("svc-a", "staging", "m1", 10m, 99m)  // Breached but in staging
        };
        var repo = Substitute.For<ISloObservationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<SloObservationStatus?>(), Arg.Any<CancellationToken>()).Returns(obs);

        var handler = new GetSloServiceRankingReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetSloServiceRankingReport.Query(TenantId, Environment: "prod"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceRanking.Should().HaveCount(1);
        result.Value.ServiceRanking[0].ComplianceRatePercent.Should().Be(100m);
        result.Value.TotalBreaches.Should().Be(0);
    }

    // ── MaxServices cap ───────────────────────────────────────────────────

    [Fact]
    public async Task MaxServices_Cap_IsRespected()
    {
        var obs = Enumerable.Range(1, 10)
            .Select(i => MakeObs($"svc-{i:D2}", "prod", "m1", 100m - i, 99m))
            .ToList();
        var repo = Substitute.For<ISloObservationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<SloObservationStatus?>(), Arg.Any<CancellationToken>()).Returns(obs);

        var handler = new GetSloServiceRankingReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetSloServiceRankingReport.Query(TenantId, MaxServices: 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceRanking.Count.Should().Be(5);
    }

    // ── TenantAvgComplianceRate aggregates service compliance ─────────────

    [Fact]
    public async Task TenantAvgComplianceRate_IsAverageOfServiceRates()
    {
        // svc-a: 100% Met; svc-b: 0% Met → avg = 50%
        var obs = new List<SloObservation>
        {
            MakeObs("svc-a", "prod", "m1", 100m, 99m),
            MakeObs("svc-b", "prod", "m1", 10m, 99m)  // Breached
        };
        var repo = Substitute.For<ISloObservationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<SloObservationStatus?>(), Arg.Any<CancellationToken>()).Returns(obs);

        var handler = new GetSloServiceRankingReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetSloServiceRankingReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantAvgComplianceRate.Should().Be(50m);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_Rejects_EmptyTenantId()
    {
        var validator = new GetSloServiceRankingReport.Validator();
        var result = validator.Validate(
            new GetSloServiceRankingReport.Query(TenantId: "", LookbackDays: 30));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Validator_Rejects_InvalidLookbackDays()
    {
        var validator = new GetSloServiceRankingReport.Validator();
        var result = validator.Validate(
            new GetSloServiceRankingReport.Query(TenantId, LookbackDays: 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LookbackDays");
    }

    [Fact]
    public void Validator_Rejects_ExcellentLessThanGood()
    {
        var validator = new GetSloServiceRankingReport.Validator();
        var result = validator.Validate(
            new GetSloServiceRankingReport.Query(TenantId,
                ExcellentThreshold: 70m, GoodThreshold: 80m));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExcellentThreshold");
    }

    [Fact]
    public void Validator_Accepts_Valid_Query()
    {
        var validator = new GetSloServiceRankingReport.Validator();
        var result = validator.Validate(
            new GetSloServiceRankingReport.Query(TenantId,
                LookbackDays: 30, MaxServices: 50,
                ExcellentThreshold: 95m, GoodThreshold: 80m));
        result.IsValid.Should().BeTrue();
    }

    // ── Report metadata ───────────────────────────────────────────────────

    [Fact]
    public async Task Report_Contains_Correct_Period()
    {
        var repo = Substitute.For<ISloObservationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<SloObservationStatus?>(), Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetSloServiceRankingReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetSloServiceRankingReport.Query(TenantId, LookbackDays: 14), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LookbackDays.Should().Be(14);
        result.Value.GeneratedAt.Should().Be(FixedNow);
        result.Value.From.Should().BeCloseTo(FixedNow.AddDays(-14), TimeSpan.FromSeconds(1));
        result.Value.To.Should().Be(FixedNow);
    }

    // ── AvgObservedValue and AvgSloTarget ─────────────────────────────────

    [Fact]
    public async Task ServiceMetrics_AvgValues_AreCorrect()
    {
        var obs = new[]
        {
            MakeObs("svc-x", "prod", "m1", 99.5m, 99.0m),
            MakeObs("svc-x", "prod", "m2", 98.5m, 99.0m)
        };
        var repo = Substitute.For<ISloObservationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<SloObservationStatus?>(), Arg.Any<CancellationToken>()).Returns(obs);

        var handler = new GetSloServiceRankingReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetSloServiceRankingReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var svc = result.Value.ServiceRanking[0];
        svc.AvgObservedValue.Should().Be(99m);
        svc.AvgSloTarget.Should().Be(99.0m);
    }
}
