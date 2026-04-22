using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetFinOpsWasteAnalysisReport;

namespace NexTraceOne.OperationalIntelligence.Tests.Cost;

/// <summary>
/// Testes unitários para Wave AG.3 — GetFinOpsWasteAnalysisReport.
/// Cobre WasteScore, WasteTier, WasteCategory, TotalEstimatedWasteUsd,
/// WasteOpportunity e Validator.
/// </summary>
public sealed class WaveAgFinOpsWasteAnalysisReportTests
{
    private const string TenantId = "tenant-ag3";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(new DateTimeOffset(2026, 4, 22, 0, 0, 0, TimeSpan.Zero));
        return clock;
    }

    private static GetFinOpsWasteAnalysisReport.Handler CreateHandler(
        GetFinOpsWasteAnalysisReport.IFinOpsWasteReader reader)
        => new(reader, CreateClock());

    private static GetFinOpsWasteAnalysisReport.IFinOpsWasteReader EmptyReader()
    {
        var r = Substitute.For<GetFinOpsWasteAnalysisReport.IFinOpsWasteReader>();
        r.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<GetFinOpsWasteAnalysisReport.ServiceWasteSignals>>([]));
        return r;
    }

    private static GetFinOpsWasteAnalysisReport.IFinOpsWasteReader ReaderWith(
        params GetFinOpsWasteAnalysisReport.ServiceWasteSignals[] signals)
    {
        var r = Substitute.For<GetFinOpsWasteAnalysisReport.IFinOpsWasteReader>();
        r.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<GetFinOpsWasteAnalysisReport.ServiceWasteSignals>>(signals));
        return r;
    }

    private static GetFinOpsWasteAnalysisReport.ServiceWasteSignals CleanService(string name = "svc-clean")
        => new(name, null,
            IsIdleResource: false, ServiceCostUsd: 50m, MedianTenantCostUsd: 100m,
            HasExcessiveNonProdCost: false, NonProdWasteCostUsd: 0m,
            HasFailedDeployments: false, FailedDeploymentCostUsd: 0m,
            HasHighSeverityDrift: false, DriftEstimatedCostImpactUsd: 0m);

    private static GetFinOpsWasteAnalysisReport.ServiceWasteSignals AllWasteSignals(string name = "svc-waste")
        => new(name, "team-w",
            IsIdleResource: true, ServiceCostUsd: 200m, MedianTenantCostUsd: 50m,
            HasExcessiveNonProdCost: true, NonProdWasteCostUsd: 80m,
            HasFailedDeployments: true, FailedDeploymentCostUsd: 50m,
            HasHighSeverityDrift: true, DriftEstimatedCostImpactUsd: 20m);

    // ── 1. Tenant sem sinais devolve relatório vazio ───────────────────────

    [Fact]
    public async Task Handler_ReturnsEmptyReport_WhenNoSignals()
    {
        var result = await CreateHandler(EmptyReader())
            .Handle(new GetFinOpsWasteAnalysisReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesAnalyzed.Should().Be(0);
        result.Value.TotalEstimatedWasteUsd.Should().Be(0m);
        result.Value.AllServices.Should().BeEmpty();
    }

    // ── 2. Sem sinais de waste → WasteTier Clean ──────────────────────────

    [Fact]
    public async Task Handler_Clean_WhenNoWasteSignals()
    {
        var result = await CreateHandler(ReaderWith(CleanService()))
            .Handle(new GetFinOpsWasteAnalysisReport.Query(TenantId), CancellationToken.None);

        result.Value.AllServices[0].Tier.Should().Be(GetFinOpsWasteAnalysisReport.WasteTier.Clean);
        result.Value.AllServices[0].WasteScore.Should().Be(0.0);
        result.Value.AllServices[0].ActiveCategories.Should().BeEmpty();
    }

    // ── 3. Idle + custo acima mediana → IdleWaste activo ──────────────────

    [Fact]
    public async Task Handler_IdleWaste_Active_WhenIdleAndCostAboveMedian()
    {
        var signal = CleanService() with { IsIdleResource = true, ServiceCostUsd = 200m, MedianTenantCostUsd = 50m };
        var result = await CreateHandler(ReaderWith(signal))
            .Handle(new GetFinOpsWasteAnalysisReport.Query(TenantId), CancellationToken.None);

        result.Value.AllServices[0].HasIdleWaste.Should().BeTrue();
        result.Value.AllServices[0].ActiveCategories.Should().Contain(GetFinOpsWasteAnalysisReport.WasteCategory.IdleWaste);
    }

    // ── 4. Idle mas custo abaixo mediana → IdleWaste inactivo ─────────────

    [Fact]
    public async Task Handler_IdleWaste_Inactive_WhenIdleButCostBelowMedian()
    {
        var signal = CleanService() with { IsIdleResource = true, ServiceCostUsd = 20m, MedianTenantCostUsd = 50m };
        var result = await CreateHandler(ReaderWith(signal))
            .Handle(new GetFinOpsWasteAnalysisReport.Query(TenantId), CancellationToken.None);

        result.Value.AllServices[0].HasIdleWaste.Should().BeFalse();
    }

    // ── 5. Todos os sinais → WasteScore = 100, tier Critical ─────────────

    [Fact]
    public async Task Handler_Critical_WhenAllWasteSignalsActive()
    {
        var result = await CreateHandler(ReaderWith(AllWasteSignals()))
            .Handle(new GetFinOpsWasteAnalysisReport.Query(TenantId), CancellationToken.None);

        result.Value.AllServices[0].WasteScore.Should().Be(100.0);
        result.Value.AllServices[0].Tier.Should().Be(GetFinOpsWasteAnalysisReport.WasteTier.Critical);
        result.Value.AllServices[0].ActiveCategories.Should().HaveCount(4);
    }

    // ── 6. OverProvisioningWaste activo ───────────────────────────────────

    [Fact]
    public async Task Handler_OverProvisioningWaste_Active_WhenExcessiveNonProdCost()
    {
        var signal = CleanService() with { HasExcessiveNonProdCost = true, NonProdWasteCostUsd = 100m };
        var result = await CreateHandler(ReaderWith(signal))
            .Handle(new GetFinOpsWasteAnalysisReport.Query(TenantId), CancellationToken.None);

        result.Value.AllServices[0].HasOverProvisioningWaste.Should().BeTrue();
        result.Value.AllServices[0].ActiveCategories.Should().Contain(
            GetFinOpsWasteAnalysisReport.WasteCategory.OverProvisioningWaste);
    }

    // ── 7. FailedDeploymentWaste activo ───────────────────────────────────

    [Fact]
    public async Task Handler_FailedDeploymentWaste_Active_WhenFailedDeploys()
    {
        var signal = CleanService() with { HasFailedDeployments = true, FailedDeploymentCostUsd = 50m };
        var result = await CreateHandler(ReaderWith(signal))
            .Handle(new GetFinOpsWasteAnalysisReport.Query(TenantId), CancellationToken.None);

        result.Value.AllServices[0].HasFailedDeploymentWaste.Should().BeTrue();
        result.Value.AllServices[0].ActiveCategories.Should().Contain(
            GetFinOpsWasteAnalysisReport.WasteCategory.FailedDeploymentWaste);
    }

    // ── 8. DriftWaste activo ──────────────────────────────────────────────

    [Fact]
    public async Task Handler_DriftWaste_Active_WhenHighSeverityDrift()
    {
        var signal = CleanService() with { HasHighSeverityDrift = true, DriftEstimatedCostImpactUsd = 30m };
        var result = await CreateHandler(ReaderWith(signal))
            .Handle(new GetFinOpsWasteAnalysisReport.Query(TenantId), CancellationToken.None);

        result.Value.AllServices[0].HasDriftWaste.Should().BeTrue();
        result.Value.AllServices[0].ActiveCategories.Should().Contain(
            GetFinOpsWasteAnalysisReport.WasteCategory.DriftWaste);
    }

    // ── 9. TotalEstimatedWasteUsd é soma de todos os serviços ─────────────

    [Fact]
    public async Task Handler_TotalEstimatedWasteUsd_IsSumOfAllServices()
    {
        var signals = new[]
        {
            AllWasteSignals("svc-1"),
            AllWasteSignals("svc-2")
        };
        var result = await CreateHandler(ReaderWith(signals))
            .Handle(new GetFinOpsWasteAnalysisReport.Query(TenantId), CancellationToken.None);

        result.Value.TotalEstimatedWasteUsd.Should().BeGreaterThan(0m);
        result.Value.TotalEstimatedWasteUsd.Should().Be(
            result.Value.AllServices.Sum(e => e.EstimatedWasteUsd));
    }

    // ── 10. WasteOpportunities contém top 10 serviços ─────────────────────

    [Fact]
    public async Task Handler_WasteOpportunities_ContainsTopServices()
    {
        var signals = Enumerable.Range(1, 15)
            .Select(i => AllWasteSignals($"svc-{i}"))
            .ToArray();

        var result = await CreateHandler(ReaderWith(signals))
            .Handle(new GetFinOpsWasteAnalysisReport.Query(TenantId), CancellationToken.None);

        result.Value.WasteOpportunities.Count.Should().Be(10);
    }

    // ── 11. WasteByCategory soma a 100% quando há waste ───────────────────

    [Fact]
    public async Task Handler_WasteByCategory_SumsTo100_WhenWasteExists()
    {
        var result = await CreateHandler(ReaderWith(AllWasteSignals()))
            .Handle(new GetFinOpsWasteAnalysisReport.Query(TenantId), CancellationToken.None);

        var dist = result.Value.WasteByCategory;
        var total = dist.IdleWastePct + dist.OverProvisioningPct +
                    dist.FailedDeploymentPct + dist.DriftWastePct;
        total.Should().BeApproximately(100.0, 1.0);
    }

    // ── 12. SignificantWasteThreshold custom altera classificação tier ────

    [Fact]
    public async Task Handler_CustomSignificantThreshold_AffectsTier()
    {
        // Single over-provisioning signal → WasteScore = 25 (OverProvisioningWeight)
        var signal = CleanService() with { HasExcessiveNonProdCost = true, NonProdWasteCostUsd = 50m };

        // With default threshold 30 → Minor (25 ≤ 30)
        var resultDefault = await CreateHandler(ReaderWith(signal))
            .Handle(new GetFinOpsWasteAnalysisReport.Query(TenantId, SignificantWasteThreshold: 30.0), CancellationToken.None);

        resultDefault.Value.AllServices[0].Tier.Should().Be(GetFinOpsWasteAnalysisReport.WasteTier.Minor);

        // With threshold 20 → Significant (25 > 20)
        var resultCustom = await CreateHandler(ReaderWith(signal))
            .Handle(new GetFinOpsWasteAnalysisReport.Query(TenantId, SignificantWasteThreshold: 20.0), CancellationToken.None);

        resultCustom.Value.AllServices[0].Tier.Should().Be(GetFinOpsWasteAnalysisReport.WasteTier.Significant);
    }

    // ── 13. Validator: MaxServices fora do range é inválido ───────────────

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void Validator_Fails_WhenMaxServicesOutOfRange(int max)
    {
        var validator = new GetFinOpsWasteAnalysisReport.Validator();
        var result = validator.Validate(new GetFinOpsWasteAnalysisReport.Query(TenantId, MaxServices: max));
        result.IsValid.Should().BeFalse();
    }

    // ── 14. Null reader devolve relatório vazio sem erro ───────────────────

    [Fact]
    public async Task Handler_NullReader_ReturnsEmptyReport()
    {
        var handler = new GetFinOpsWasteAnalysisReport.Handler(
            new GetFinOpsWasteAnalysisReport.NullFinOpsWasteReader(), CreateClock());
        var result = await handler.Handle(
            new GetFinOpsWasteAnalysisReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesAnalyzed.Should().Be(0);
    }

    // ── 15. Validator: TenantId vazio é inválido ──────────────────────────

    [Fact]
    public void Validator_Fails_WhenTenantIdEmpty()
    {
        var validator = new GetFinOpsWasteAnalysisReport.Validator();
        var result = validator.Validate(new GetFinOpsWasteAnalysisReport.Query(string.Empty));
        result.IsValid.Should().BeFalse();
    }
}
