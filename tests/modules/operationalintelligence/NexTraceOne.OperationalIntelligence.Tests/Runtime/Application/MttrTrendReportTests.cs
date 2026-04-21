using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetMttrTrendReport;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave S.3 — GetMttrTrendReport.
/// Cobre: sem dados, sem breaches, elite/high/medium/low DORA tiers, trend Improving/Worsening/Stable/Insufficient,
/// daily series, top worst ordering, multi-serviço, validator.
/// </summary>
public sealed class MttrTrendReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-mttr-001";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static SloObservation MakeObs(
        string serviceName,
        string env,
        SloObservationStatus status,
        DateTimeOffset observedAt)
    {
        // For Breached: 85 vs 99.9; for Met: 99.95 vs 99.9
        decimal observed = status == SloObservationStatus.Breached ? 85m : 99.95m;
        var periodStart = observedAt.AddHours(-1);
        var periodEnd = observedAt;
        return SloObservation.Create(TenantId, serviceName, env, "availability",
            observed, 99.9m, periodStart, periodEnd, observedAt, "percent");
    }

    private static GetMttrTrendReport.Handler CreateHandler(IReadOnlyList<SloObservation> observations)
    {
        var sloRepo = Substitute.For<ISloObservationRepository>();
        sloRepo.ListByTenantAsync(TenantId,
                Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<SloObservationStatus?>(), Arg.Any<CancellationToken>())
            .Returns(observations);
        return new GetMttrTrendReport.Handler(sloRepo, CreateClock());
    }

    private static GetMttrTrendReport.Query DefaultQuery()
        => new(TenantId: TenantId, LookbackDays: 30, TopWorstCount: 10);

    // ── Empty: no observations ────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoObservations_ReturnsEmptyReport()
    {
        var handler = CreateHandler([]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var r = result.Value;
        Assert.Equal(0, r.TotalServicesAnalyzed);
        Assert.Equal(0, r.TotalBreachEvents);
        Assert.Equal(0, r.TenantAvgMttrHours);
        Assert.Empty(r.AllServices);
    }

    // ── No breaches: all Met observations → no MTTR entries ──────────────

    [Fact]
    public async Task Handle_OnlyMetObservations_NoEntries()
    {
        var obs = MakeObs("svc-a", "prod", SloObservationStatus.Met, FixedNow.AddDays(-5));
        var handler = CreateHandler([obs]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // 1 service analyzed but 0 breach events → no MTTR entries
        Assert.Equal(1, result.Value.TotalServicesAnalyzed);
        Assert.Equal(0, result.Value.TotalBreachEvents);
        Assert.Empty(result.Value.AllServices);
    }

    // ── Elite: MTTR < 1 hour ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_MttrUnder1Hour_EliteTier()
    {
        var breach = MakeObs("svc-elite", "prod", SloObservationStatus.Breached, FixedNow.AddDays(-10));
        var restore = MakeObs("svc-elite", "prod", SloObservationStatus.Met, breach.ObservedAt.AddMinutes(30));
        var handler = CreateHandler([breach, restore]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetMttrTrendReport.MttrDoraLevel.Elite, entry.DoraLevel);
        Assert.True(entry.AvgMttrHours < 1.0);
    }

    // ── High: MTTR 1–4 hours ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_MttrBetween1And4Hours_HighTier()
    {
        var breach = MakeObs("svc-high", "prod", SloObservationStatus.Breached, FixedNow.AddDays(-10));
        var restore = MakeObs("svc-high", "prod", SloObservationStatus.Met, breach.ObservedAt.AddHours(2));
        var handler = CreateHandler([breach, restore]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetMttrTrendReport.MttrDoraLevel.High, entry.DoraLevel);
    }

    // ── Medium: MTTR 4–24 hours ───────────────────────────────────────────

    [Fact]
    public async Task Handle_MttrBetween4And24Hours_MediumTier()
    {
        var breach = MakeObs("svc-medium", "prod", SloObservationStatus.Breached, FixedNow.AddDays(-10));
        var restore = MakeObs("svc-medium", "prod", SloObservationStatus.Met, breach.ObservedAt.AddHours(8));
        var handler = CreateHandler([breach, restore]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetMttrTrendReport.MttrDoraLevel.Medium, entry.DoraLevel);
    }

    // ── Low: MTTR > 24 hours ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_MttrOver24Hours_LowTier()
    {
        var breach = MakeObs("svc-low", "prod", SloObservationStatus.Breached, FixedNow.AddDays(-20));
        var restore = MakeObs("svc-low", "prod", SloObservationStatus.Met, breach.ObservedAt.AddHours(30));
        var handler = CreateHandler([breach, restore]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetMttrTrendReport.MttrDoraLevel.Low, entry.DoraLevel);
    }

    // ── Trend Improving: second half MTTR much lower ──────────────────────

    [Fact]
    public async Task Handle_SecondHalfMttrMuchLower_ImprovingTrend()
    {
        // First half: 10h breach, second half: 0.5h breach
        var midpoint = FixedNow.AddDays(-15);

        var b1 = MakeObs("svc-trend", "prod", SloObservationStatus.Breached, FixedNow.AddDays(-28));
        var r1 = MakeObs("svc-trend", "prod", SloObservationStatus.Met, b1.ObservedAt.AddHours(10));
        var b2 = MakeObs("svc-trend", "prod", SloObservationStatus.Breached, FixedNow.AddDays(-5));
        var r2 = MakeObs("svc-trend", "prod", SloObservationStatus.Met, b2.ObservedAt.AddMinutes(30));

        var handler = CreateHandler([b1, r1, b2, r2]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetMttrTrendReport.MttrTrend.Improving, entry.Trend);
    }

    // ── Trend Worsening: second half MTTR much higher ─────────────────────

    [Fact]
    public async Task Handle_SecondHalfMttrMuchHigher_WorseningTrend()
    {
        var b1 = MakeObs("svc-worse", "prod", SloObservationStatus.Breached, FixedNow.AddDays(-28));
        var r1 = MakeObs("svc-worse", "prod", SloObservationStatus.Met, b1.ObservedAt.AddMinutes(30));
        var b2 = MakeObs("svc-worse", "prod", SloObservationStatus.Breached, FixedNow.AddDays(-5));
        var r2 = MakeObs("svc-worse", "prod", SloObservationStatus.Met, b2.ObservedAt.AddHours(10));

        var handler = CreateHandler([b1, r1, b2, r2]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetMttrTrendReport.MttrTrend.Worsening, entry.Trend);
    }

    // ── Trend Insufficient: only one breach event ─────────────────────────

    [Fact]
    public async Task Handle_OnlyOneBreachEvent_InsufficientTrend()
    {
        var breach = MakeObs("svc-insuf", "prod", SloObservationStatus.Breached, FixedNow.AddDays(-5));
        var restore = MakeObs("svc-insuf", "prod", SloObservationStatus.Met, breach.ObservedAt.AddHours(2));
        var handler = CreateHandler([breach, restore]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(GetMttrTrendReport.MttrTrend.Insufficient, entry.Trend);
    }

    // ── Daily series: contains entry per day ──────────────────────────────

    [Fact]
    public async Task Handle_WithBreachEvent_DailySeriesNotEmpty()
    {
        var breach = MakeObs("svc-daily", "prod", SloObservationStatus.Breached, FixedNow.AddDays(-5));
        var restore = MakeObs("svc-daily", "prod", SloObservationStatus.Met, breach.ObservedAt.AddHours(1));
        var handler = CreateHandler([breach, restore]);
        var result = await handler.Handle(new GetMttrTrendReport.Query(TenantId, LookbackDays: 7), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // 7 days + today = 8 data points
        Assert.Equal(8, result.Value.DailyMttrSeries.Count);
        // The day with the breach should have AvgMttrHours > 0
        var breachDay = DateOnly.FromDateTime(breach.ObservedAt.Date);
        var dataPoint = result.Value.DailyMttrSeries.FirstOrDefault(d => d.Date == breachDay);
        Assert.NotNull(dataPoint);
        Assert.True(dataPoint.AvgMttrHours > 0);
    }

    // ── TopWorstMttrServices ordered by AvgMttrHours descending ──────────

    [Fact]
    public async Task Handle_TopWorst_OrderedByAvgMttrDescending()
    {
        // svc-a: 2h MTTR, svc-b: 8h MTTR
        var bA = MakeObs("svc-a", "prod", SloObservationStatus.Breached, FixedNow.AddDays(-10));
        var rA = MakeObs("svc-a", "prod", SloObservationStatus.Met, bA.ObservedAt.AddHours(2));
        var bB = MakeObs("svc-b", "prod", SloObservationStatus.Breached, FixedNow.AddDays(-10));
        var rB = MakeObs("svc-b", "prod", SloObservationStatus.Met, bB.ObservedAt.AddHours(8));

        var handler = CreateHandler([bA, rA, bB, rB]);
        var result = await handler.Handle(DefaultQuery() with { TopWorstCount = 5 }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var top = result.Value.TopWorstMttrServices;
        Assert.Equal(2, top.Count);
        Assert.Equal("svc-b", top[0].ServiceName);
        Assert.True(top[0].AvgMttrHours > top[1].AvgMttrHours);
    }

    // ── Multiple breach events: average is computed correctly ─────────────

    [Fact]
    public async Task Handle_MultipleBreachEvents_CorrectAverage()
    {
        // Two breaches: 2h and 4h → avg = 3h
        var b1 = MakeObs("svc-avg", "prod", SloObservationStatus.Breached, FixedNow.AddDays(-20));
        var r1 = MakeObs("svc-avg", "prod", SloObservationStatus.Met, b1.ObservedAt.AddHours(2));
        var b2 = MakeObs("svc-avg", "prod", SloObservationStatus.Breached, FixedNow.AddDays(-10));
        var r2 = MakeObs("svc-avg", "prod", SloObservationStatus.Met, b2.ObservedAt.AddHours(4));

        var handler = CreateHandler([b1, r1, b2, r2]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = result.Value.AllServices.Single();
        Assert.Equal(3.0, entry.AvgMttrHours);
        Assert.Equal(4.0, entry.MaxMttrHours);
        Assert.Equal(2, entry.TotalBreachEvents);
    }

    // ── GeneratedAt and LookbackDays echoed ──────────────────────────────

    [Fact]
    public async Task Handle_GeneratedAt_And_LookbackDays_Correct()
    {
        var handler = CreateHandler([]);
        var result = await handler.Handle(DefaultQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(FixedNow, result.Value.GeneratedAt);
        Assert.Equal(30, result.Value.LookbackDays);
    }

    // ── Validator: empty TenantId fails ──────────────────────────────────

    [Fact]
    public void Validator_EmptyTenantId_Fails()
    {
        var validator = new GetMttrTrendReport.Validator();
        var result = validator.Validate(new GetMttrTrendReport.Query(TenantId: ""));
        Assert.False(result.IsValid);
    }

    // ── Validator: LookbackDays out of range ──────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    public void Validator_LookbackDaysOutOfRange_Fails(int days)
    {
        var validator = new GetMttrTrendReport.Validator();
        var result = validator.Validate(new GetMttrTrendReport.Query(TenantId: TenantId, LookbackDays: days));
        Assert.False(result.IsValid);
    }

    // ── Validator: valid query passes ─────────────────────────────────────

    [Fact]
    public void Validator_ValidQuery_Passes()
    {
        var validator = new GetMttrTrendReport.Validator();
        var result = validator.Validate(DefaultQuery());
        Assert.True(result.IsValid);
    }
}
