using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetEnvironmentCostComparisonReport;

namespace NexTraceOne.OperationalIntelligence.Tests.Cost;

/// <summary>
/// Testes unitários para Wave AG.1 — GetEnvironmentCostComparisonReport.
/// Cobre EnvironmentEfficiencyTier, NonProdToProdRatio, NonProdWasteCostUsd,
/// TotalNonProdWasteUsd, DistributionByTier e Validator.
/// </summary>
public sealed class WaveAgEnvironmentCostComparisonReportTests
{
    private const string TenantId = "tenant-ag1";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(new DateTimeOffset(2026, 4, 22, 0, 0, 0, TimeSpan.Zero));
        return clock;
    }

    private static GetEnvironmentCostComparisonReport.Handler CreateHandler(
        GetEnvironmentCostComparisonReport.IEnvironmentCostComparisonReader reader)
        => new(reader, CreateClock());

    private static GetEnvironmentCostComparisonReport.IEnvironmentCostComparisonReader EmptyReader()
    {
        var r = Substitute.For<GetEnvironmentCostComparisonReport.IEnvironmentCostComparisonReader>();
        r.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<GetEnvironmentCostComparisonReport.EnvironmentCostRecord>>([]));
        return r;
    }

    private static GetEnvironmentCostComparisonReport.IEnvironmentCostComparisonReader ReaderWith(
        params GetEnvironmentCostComparisonReport.EnvironmentCostRecord[] records)
    {
        var r = Substitute.For<GetEnvironmentCostComparisonReport.IEnvironmentCostComparisonReader>();
        r.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<GetEnvironmentCostComparisonReport.EnvironmentCostRecord>>(records));
        return r;
    }

    // ── 1. Tenant sem registos devolve relatório vazio ─────────────────────

    [Fact]
    public async Task Handler_ReturnsEmptyReport_WhenNoRecords()
    {
        var result = await CreateHandler(EmptyReader())
            .Handle(new GetEnvironmentCostComparisonReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesAnalyzed.Should().Be(0);
        result.Value.TotalNonProdWasteUsd.Should().Be(0m);
        result.Value.AllServices.Should().BeEmpty();
    }

    // ── 2. Serviço com ratio ≤ 0.5 → Optimal ─────────────────────────────

    [Fact]
    public async Task Handler_Optimal_WhenNonProdRatioLeq05()
    {
        var records = new[]
        {
            new GetEnvironmentCostComparisonReport.EnvironmentCostRecord("svc-a", "team-a", "production", 100m),
            new GetEnvironmentCostComparisonReport.EnvironmentCostRecord("svc-a", "team-a", "staging", 40m)
        };
        var result = await CreateHandler(ReaderWith(records))
            .Handle(new GetEnvironmentCostComparisonReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var entry = result.Value.AllServices[0];
        entry.Tier.Should().Be(GetEnvironmentCostComparisonReport.EnvironmentEfficiencyTier.Optimal);
        entry.NonProdToProdRatio.Should().Be(0.4);
    }

    // ── 3. Serviço com ratio 0.6–1.0 → Acceptable ──────────────────────────

    [Fact]
    public async Task Handler_Acceptable_WhenNonProdRatioBetween05And10()
    {
        var records = new[]
        {
            new GetEnvironmentCostComparisonReport.EnvironmentCostRecord("svc-b", "team-b", "production", 100m),
            new GetEnvironmentCostComparisonReport.EnvironmentCostRecord("svc-b", "team-b", "staging", 80m)
        };
        var result = await CreateHandler(ReaderWith(records))
            .Handle(new GetEnvironmentCostComparisonReport.Query(TenantId), CancellationToken.None);

        result.Value.AllServices[0].Tier.Should().Be(
            GetEnvironmentCostComparisonReport.EnvironmentEfficiencyTier.Acceptable);
    }

    // ── 4. Serviço com ratio 1.5 → Overprovisioned ────────────────────────

    [Fact]
    public async Task Handler_Overprovisioned_WhenNonProdRatioBetween10And20()
    {
        var records = new[]
        {
            new GetEnvironmentCostComparisonReport.EnvironmentCostRecord("svc-c", null, "production", 100m),
            new GetEnvironmentCostComparisonReport.EnvironmentCostRecord("svc-c", null, "dev", 80m),
            new GetEnvironmentCostComparisonReport.EnvironmentCostRecord("svc-c", null, "staging", 70m)
        };
        var result = await CreateHandler(ReaderWith(records))
            .Handle(new GetEnvironmentCostComparisonReport.Query(TenantId), CancellationToken.None);

        result.Value.AllServices[0].Tier.Should().Be(
            GetEnvironmentCostComparisonReport.EnvironmentEfficiencyTier.Overprovisioned);
        result.Value.AllServices[0].NonProdCostUsd.Should().Be(150m);
    }

    // ── 5. Serviço com ratio > 2.0 → WasteAlert ───────────────────────────

    [Fact]
    public async Task Handler_WasteAlert_WhenNonProdRatioAbove20()
    {
        var records = new[]
        {
            new GetEnvironmentCostComparisonReport.EnvironmentCostRecord("svc-d", "team-d", "production", 50m),
            new GetEnvironmentCostComparisonReport.EnvironmentCostRecord("svc-d", "team-d", "staging", 110m),
            new GetEnvironmentCostComparisonReport.EnvironmentCostRecord("svc-d", "team-d", "dev", 50m)
        };
        var result = await CreateHandler(ReaderWith(records))
            .Handle(new GetEnvironmentCostComparisonReport.Query(TenantId), CancellationToken.None);

        result.Value.AllServices[0].Tier.Should().Be(
            GetEnvironmentCostComparisonReport.EnvironmentEfficiencyTier.WasteAlert);
    }

    // ── 6. NonProdWasteCostUsd calculado correctamente ─────────────────────

    [Fact]
    public async Task Handler_CalculatesNonProdWasteCostUsd_Correctly()
    {
        // prod=100, nonProd=200, expected=50 (0.5*100), waste=150
        var records = new[]
        {
            new GetEnvironmentCostComparisonReport.EnvironmentCostRecord("svc-e", null, "production", 100m),
            new GetEnvironmentCostComparisonReport.EnvironmentCostRecord("svc-e", null, "staging", 200m)
        };
        var result = await CreateHandler(ReaderWith(records))
            .Handle(new GetEnvironmentCostComparisonReport.Query(TenantId, ExpectedNonProdRatio: 0.5), CancellationToken.None);

        result.Value.AllServices[0].NonProdWasteCostUsd.Should().Be(150m);
    }

    // ── 7. Serviço sem custo em prod é excluído ────────────────────────────

    [Fact]
    public async Task Handler_ExcludesService_WhenNoProdCost()
    {
        var records = new[]
        {
            new GetEnvironmentCostComparisonReport.EnvironmentCostRecord("svc-f", null, "staging", 100m)
        };
        var result = await CreateHandler(ReaderWith(records))
            .Handle(new GetEnvironmentCostComparisonReport.Query(TenantId), CancellationToken.None);

        result.Value.TotalServicesAnalyzed.Should().Be(0);
    }

    // ── 8. Distribuição por tier correcta ─────────────────────────────────

    [Fact]
    public async Task Handler_DistributionByTier_IsCorrect()
    {
        var records = new GetEnvironmentCostComparisonReport.EnvironmentCostRecord[]
        {
            // Optimal (ratio 0.4)
            new("svc-1", null, "production", 100m),
            new("svc-1", null, "staging", 40m),
            // WasteAlert (ratio 3.0)
            new("svc-2", null, "production", 50m),
            new("svc-2", null, "staging", 150m)
        };
        var result = await CreateHandler(ReaderWith(records))
            .Handle(new GetEnvironmentCostComparisonReport.Query(TenantId), CancellationToken.None);

        result.Value.DistributionByTier.OptimalCount.Should().Be(1);
        result.Value.DistributionByTier.WasteAlertCount.Should().Be(1);
    }

    // ── 9. TotalNonProdWasteUsd é soma de waste individuais ───────────────

    [Fact]
    public async Task Handler_TotalNonProdWasteUsd_IsSumOfIndividualWaste()
    {
        var records = new GetEnvironmentCostComparisonReport.EnvironmentCostRecord[]
        {
            new("svc-a", null, "production", 100m),
            new("svc-a", null, "staging", 200m),   // waste = 150
            new("svc-b", null, "production", 50m),
            new("svc-b", null, "staging", 120m)    // waste = 95
        };
        var result = await CreateHandler(ReaderWith(records))
            .Handle(new GetEnvironmentCostComparisonReport.Query(TenantId, ExpectedNonProdRatio: 0.5), CancellationToken.None);

        result.Value.TotalNonProdWasteUsd.Should().Be(245m);
    }

    // ── 10. TopWasteServices respeita TopServicesCount ────────────────────

    [Fact]
    public async Task Handler_TopWasteServices_RespectsMaxCount()
    {
        var records = Enumerable.Range(1, 20)
            .SelectMany(i => new GetEnvironmentCostComparisonReport.EnvironmentCostRecord[]
            {
                new($"svc-{i}", null, "production", 100m),
                new($"svc-{i}", null, "staging", 300m)
            })
            .ToArray();

        var result = await CreateHandler(ReaderWith(records))
            .Handle(new GetEnvironmentCostComparisonReport.Query(TenantId, TopServicesCount: 5), CancellationToken.None);

        result.Value.TopWasteServices.Count.Should().Be(5);
    }

    // ── 11. Validator: TenantId vazio é inválido ───────────────────────────

    [Fact]
    public void Validator_Fails_WhenTenantIdEmpty()
    {
        var validator = new GetEnvironmentCostComparisonReport.Validator();
        var result = validator.Validate(new GetEnvironmentCostComparisonReport.Query(string.Empty));
        result.IsValid.Should().BeFalse();
    }

    // ── 12. Validator: LookbackDays fora do intervalo é inválido ──────────

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(91)]
    public void Validator_Fails_WhenLookbackDaysOutOfRange(int days)
    {
        var validator = new GetEnvironmentCostComparisonReport.Validator();
        var result = validator.Validate(new GetEnvironmentCostComparisonReport.Query(TenantId, LookbackDays: days));
        result.IsValid.Should().BeFalse();
    }

    // ── 13. Null reader devolve relatório vazio sem erro ───────────────────

    [Fact]
    public async Task Handler_NullReader_ReturnsEmptyReport()
    {
        var handler = new GetEnvironmentCostComparisonReport.Handler(
            new GetEnvironmentCostComparisonReport.NullEnvironmentCostComparisonReader(), CreateClock());
        var result = await handler.Handle(
            new GetEnvironmentCostComparisonReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesAnalyzed.Should().Be(0);
    }
}
