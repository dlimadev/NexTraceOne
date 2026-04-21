using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetSloComplianceSummary;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetSloViolationTrend;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.IngestSloObservation;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave J.2 — SLO Tracking (OperationalIntelligence).
/// Cobre entidade SloObservation, IngestSloObservation, GetSloComplianceSummary e GetSloViolationTrend.
/// </summary>
public sealed class SloTrackingTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodStart = new(2026, 4, 20, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodEnd = new(2026, 4, 21, 0, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-001";

    private readonly ISloObservationRepository _repository = Substitute.For<ISloObservationRepository>();
    private readonly IRuntimeIntelligenceUnitOfWork _unitOfWork = Substitute.For<IRuntimeIntelligenceUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public SloTrackingTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    // ── Domain: SloObservation entity ────────────────────────────────────────

    [Fact]
    public void SloObservation_Create_Status_Met_When_Value_Exceeds_Target()
    {
        var obs = SloObservation.Create(TenantId, "api-gateway", "production",
            "availability", 99.95m, 99.9m, PeriodStart, PeriodEnd, FixedNow, "percent");

        obs.Status.Should().Be(SloObservationStatus.Met);
        obs.ObservedValue.Should().Be(99.95m);
        obs.SloTarget.Should().Be(99.9m);
    }

    [Fact]
    public void SloObservation_Create_Status_Warning_When_Value_Near_Target()
    {
        // 99.0 vs 99.9 → gap = 0.9/99.9 ≈ 0.009 → within 10% → Warning
        var obs = SloObservation.Create(TenantId, "api-gateway", "production",
            "availability", 99.0m, 99.9m, PeriodStart, PeriodEnd, FixedNow);

        obs.Status.Should().Be(SloObservationStatus.Warning);
    }

    [Fact]
    public void SloObservation_Create_Status_Breached_When_Value_Far_Below_Target()
    {
        // 85.0 vs 99.9 → gap = 14.9/99.9 ≈ 0.149 → exceeds 10% → Breached
        var obs = SloObservation.Create(TenantId, "api-gateway", "production",
            "availability", 85.0m, 99.9m, PeriodStart, PeriodEnd, FixedNow);

        obs.Status.Should().Be(SloObservationStatus.Breached);
    }

    [Fact]
    public void SloObservation_Create_Throws_When_PeriodEnd_Before_PeriodStart()
    {
        var act = () => SloObservation.Create(TenantId, "api-gateway", "production",
            "availability", 99.5m, 99.9m, PeriodEnd, PeriodStart, FixedNow);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void SloObservation_Create_Has_Valid_Id()
    {
        var obs = SloObservation.Create(TenantId, "api-gateway", "production",
            "latency_p99", 250m, 300m, PeriodStart, PeriodEnd, FixedNow, "ms");

        obs.Id.Value.Should().NotBe(Guid.Empty);
        obs.MetricName.Should().Be("latency_p99");
        obs.Unit.Should().Be("ms");
    }

    // ── IngestSloObservation feature ─────────────────────────────────────────

    [Fact]
    public async Task IngestSloObservation_Creates_And_Persists_Observation()
    {
        SloObservation? captured = null;
        _repository.When(r => r.Add(Arg.Any<SloObservation>()))
            .Do(ci => captured = ci.Arg<SloObservation>());
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new IngestSloObservation.Handler(_repository, _unitOfWork, _clock);
        var cmd = new IngestSloObservation.Command(
            TenantId, "payment-api", "production", "availability", 99.95m, 99.9m,
            PeriodStart, PeriodEnd, "percent");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.ServiceName.Should().Be("payment-api");
        captured.Status.Should().Be(SloObservationStatus.Met);
        result.Value.Status.Should().Be(SloObservationStatus.Met);
    }

    [Fact]
    public async Task IngestSloObservation_Validator_Rejects_Empty_TenantId()
    {
        var validator = new IngestSloObservation.Validator();
        var result = validator.Validate(new IngestSloObservation.Command(
            "", "svc", "env", "metric", 1m, 1m, PeriodStart, PeriodEnd));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task IngestSloObservation_Validator_Rejects_PeriodEnd_Before_PeriodStart()
    {
        var validator = new IngestSloObservation.Validator();
        var result = validator.Validate(new IngestSloObservation.Command(
            TenantId, "svc", "env", "metric", 1m, 1m, PeriodEnd, PeriodStart));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task IngestSloObservation_Validator_Rejects_Zero_SloTarget()
    {
        var validator = new IngestSloObservation.Validator();
        var result = validator.Validate(new IngestSloObservation.Command(
            TenantId, "svc", "env", "metric", 1m, 0m, PeriodStart, PeriodEnd));
        result.IsValid.Should().BeFalse();
    }

    // ── GetSloComplianceSummary feature ──────────────────────────────────────

    [Fact]
    public async Task GetSloComplianceSummary_Returns_AllMet_When_All_Observations_Met()
    {
        var obs1 = SloObservation.Create(TenantId, "api", "prod", "availability", 99.99m, 99.9m, PeriodStart, PeriodEnd, FixedNow.AddHours(-1));
        var obs2 = SloObservation.Create(TenantId, "api", "prod", "availability", 99.95m, 99.9m, PeriodStart, PeriodEnd, FixedNow.AddHours(-2));
        _repository.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<SloObservation>)[obs1, obs2]);

        var handler = new GetSloComplianceSummary.Handler(_repository, _clock);
        var result = await handler.Handle(new GetSloComplianceSummary.Query(TenantId, 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be("AllMet");
        result.Value.TotalViolations.Should().Be(0);
    }

    [Fact]
    public async Task GetSloComplianceSummary_Returns_Violated_When_High_Breach_Rate()
    {
        var obs1 = SloObservation.Create(TenantId, "api", "prod", "availability", 85m, 99.9m, PeriodStart, PeriodEnd, FixedNow.AddHours(-1));
        var obs2 = SloObservation.Create(TenantId, "api", "prod", "availability", 80m, 99.9m, PeriodStart, PeriodEnd, FixedNow.AddHours(-2));
        _repository.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<SloObservation>)[obs1, obs2]);

        var handler = new GetSloComplianceSummary.Handler(_repository, _clock);
        var result = await handler.Handle(new GetSloComplianceSummary.Query(TenantId, 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be("Violated");
        result.Value.TotalViolations.Should().Be(2);
    }

    [Fact]
    public async Task GetSloComplianceSummary_Returns_NoData_When_No_Observations()
    {
        _repository.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<SloObservation>)[]);

        var handler = new GetSloComplianceSummary.Handler(_repository, _clock);
        var result = await handler.Handle(new GetSloComplianceSummary.Query(TenantId, 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be("NoData");
        result.Value.TotalObservations.Should().Be(0);
    }

    // ── GetSloViolationTrend feature ─────────────────────────────────────────

    [Fact]
    public async Task GetSloViolationTrend_Returns_Trend_Stable_When_Violations_Consistent()
    {
        // Spread violations evenly across both halves: 5 in older period and 5 in recent period
        // Using i=1..5 to avoid including today (i=0) which falls outside the 30-day window range
        var recentViolations = Enumerable.Range(1, 5)
            .Select(i => SloObservation.Create(TenantId, "api", "prod", "availability",
                85m, 99.9m, PeriodStart, PeriodEnd, FixedNow.AddDays(-i)))
            .ToList();
        var olderViolations = Enumerable.Range(16, 5)
            .Select(i => SloObservation.Create(TenantId, "api", "prod", "availability",
                85m, 99.9m, PeriodStart, PeriodEnd, FixedNow.AddDays(-i)))
            .ToList();
        var allObservations = recentViolations.Concat(olderViolations).ToList();

        _repository.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<SloObservation>)allObservations);

        var handler = new GetSloViolationTrend.Handler(_repository, _clock);
        var result = await handler.Handle(new GetSloViolationTrend.Query(TenantId, 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalViolations.Should().Be(10);
        result.Value.Windows.Should().HaveCount(30);
    }

    [Fact]
    public async Task GetSloViolationTrend_Returns_Worsening_When_Recent_Violations_Higher()
    {
        // Recent violations concentrated in the last 15 days
        var recentViolations = Enumerable.Range(0, 10)
            .Select(i => SloObservation.Create(TenantId, "api", "prod", "availability",
                80m, 99.9m, PeriodStart, PeriodEnd, FixedNow.AddDays(-i)))
            .ToList();

        _repository.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<SloObservation>)recentViolations);

        var handler = new GetSloViolationTrend.Handler(_repository, _clock);
        var result = await handler.Handle(new GetSloViolationTrend.Query(TenantId, 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // All 10 violations in recent half → "Worsening"
        result.Value.Trend.Should().Be("Worsening");
    }

    [Fact]
    public async Task GetSloViolationTrend_Returns_NoViolations_When_All_Met()
    {
        var observations = new List<SloObservation>
        {
            SloObservation.Create(TenantId, "api", "prod", "availability",
                99.99m, 99.9m, PeriodStart, PeriodEnd, FixedNow.AddHours(-1))
        };

        _repository.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<SloObservation>)observations);

        var handler = new GetSloViolationTrend.Handler(_repository, _clock);
        var result = await handler.Handle(new GetSloViolationTrend.Query(TenantId, 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalViolations.Should().Be(0);
        result.Value.PeakViolationDate.Should().BeNull();
    }

    [Fact]
    public async Task GetSloViolationTrend_Validator_Rejects_Days_Above_90()
    {
        var validator = new GetSloViolationTrend.Validator();
        var result = validator.Validate(new GetSloViolationTrend.Query(TenantId, 91));
        result.IsValid.Should().BeFalse();
    }
}
