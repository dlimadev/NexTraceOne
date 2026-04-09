using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Domain;

/// <summary>
/// Testes unitários para a entidade CostAttribution.
/// Valida factory Compute, validação de breakdown de custos, guard clauses e defaults.
/// </summary>
public sealed class CostAttributionTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodStart = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodEnd = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    // ── Factory method: Compute — valid scenarios ──

    [Fact]
    public void Compute_ValidInputs_ShouldCreateAttribution()
    {
        var attribution = CreateValid();

        attribution.Id.Value.Should().NotBe(Guid.Empty);
        attribution.Dimension.Should().Be(CostAttributionDimension.Service);
        attribution.DimensionKey.Should().Be("payment-service");
        attribution.DimensionLabel.Should().Be("Payment Service");
        attribution.PeriodStart.Should().Be(PeriodStart);
        attribution.PeriodEnd.Should().Be(PeriodEnd);
        attribution.TotalCost.Should().Be(1000m);
        attribution.ComputeCost.Should().Be(500m);
        attribution.StorageCost.Should().Be(200m);
        attribution.NetworkCost.Should().Be(100m);
        attribution.OtherCost.Should().Be(200m);
        attribution.Currency.Should().Be("USD");
        attribution.AttributionMethod.Should().Be("telemetry-based");
        attribution.ComputedAt.Should().Be(FixedNow);
        attribution.TenantId.Should().Be("tenant1");
    }

    [Fact]
    public void Compute_AllDimensions_ShouldBeAccepted()
    {
        foreach (var dimension in Enum.GetValues<CostAttributionDimension>())
        {
            var attribution = CostAttribution.Compute(
                dimension: dimension,
                dimensionKey: "test-key",
                dimensionLabel: null,
                periodStart: PeriodStart,
                periodEnd: PeriodEnd,
                totalCost: 100m,
                computeCost: 25m,
                storageCost: 25m,
                networkCost: 25m,
                otherCost: 25m,
                currency: "EUR",
                costBreakdown: null,
                attributionMethod: null,
                dataSources: null,
                tenantId: null,
                now: FixedNow);

            attribution.Dimension.Should().Be(dimension);
        }
    }

    [Fact]
    public void Compute_DefaultCurrency_ShouldBeUsedWhenProvided()
    {
        var attribution = CostAttribution.Compute(
            dimension: CostAttributionDimension.Team,
            dimensionKey: "platform-team",
            dimensionLabel: null,
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            totalCost: 0m,
            computeCost: 0m,
            storageCost: 0m,
            networkCost: 0m,
            otherCost: 0m,
            currency: "USD",
            costBreakdown: null,
            attributionMethod: null,
            dataSources: null,
            tenantId: null,
            now: FixedNow);

        attribution.Currency.Should().Be("USD");
    }

    [Fact]
    public void Compute_ZeroCosts_ShouldBeValid()
    {
        var attribution = CostAttribution.Compute(
            dimension: CostAttributionDimension.Domain,
            dimensionKey: "inactive-domain",
            dimensionLabel: "Inactive Domain",
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            totalCost: 0m,
            computeCost: 0m,
            storageCost: 0m,
            networkCost: 0m,
            otherCost: 0m,
            currency: "USD",
            costBreakdown: null,
            attributionMethod: "direct",
            dataSources: null,
            tenantId: null,
            now: FixedNow);

        attribution.TotalCost.Should().Be(0m);
        attribution.Dimension.Should().Be(CostAttributionDimension.Domain);
    }

    [Fact]
    public void Compute_NullOptionalFields_ShouldBeValid()
    {
        var attribution = CostAttribution.Compute(
            dimension: CostAttributionDimension.Change,
            dimensionKey: "release-v1.2.0",
            dimensionLabel: null,
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            totalCost: 50m,
            computeCost: 50m,
            storageCost: 0m,
            networkCost: 0m,
            otherCost: 0m,
            currency: "GBP",
            costBreakdown: null,
            attributionMethod: null,
            dataSources: null,
            tenantId: null,
            now: FixedNow);

        attribution.DimensionLabel.Should().BeNull();
        attribution.CostBreakdown.Should().BeNull();
        attribution.AttributionMethod.Should().BeNull();
        attribution.DataSources.Should().BeNull();
        attribution.TenantId.Should().BeNull();
    }

    [Fact]
    public void Compute_TrimsStrings()
    {
        var attribution = CostAttribution.Compute(
            dimension: CostAttributionDimension.Service,
            dimensionKey: "  svc-name  ",
            dimensionLabel: "  Service Label  ",
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            totalCost: 10m,
            computeCost: 10m,
            storageCost: 0m,
            networkCost: 0m,
            otherCost: 0m,
            currency: " EUR ",
            costBreakdown: null,
            attributionMethod: "  proportional  ",
            dataSources: null,
            tenantId: "  t1  ",
            now: FixedNow);

        attribution.DimensionKey.Should().Be("svc-name");
        attribution.DimensionLabel.Should().Be("Service Label");
        attribution.Currency.Should().Be("EUR");
        attribution.AttributionMethod.Should().Be("proportional");
        attribution.TenantId.Should().Be("t1");
    }

    // ── Cost breakdown validation ──

    [Fact]
    public void Compute_TotalCostMismatch_ShouldThrow()
    {
        var act = () => CostAttribution.Compute(
            dimension: CostAttributionDimension.Service,
            dimensionKey: "svc",
            dimensionLabel: null,
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            totalCost: 999m,
            computeCost: 100m,
            storageCost: 100m,
            networkCost: 100m,
            otherCost: 100m,
            currency: "USD",
            costBreakdown: null,
            attributionMethod: null,
            dataSources: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>()
            .Which.Message.Should().Contain("Total cost");
    }

    [Fact]
    public void Compute_NegativeTotalCost_ShouldThrow()
    {
        var act = () => CostAttribution.Compute(
            dimension: CostAttributionDimension.Team,
            dimensionKey: "team-x",
            dimensionLabel: null,
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            totalCost: -10m,
            computeCost: 0m,
            storageCost: 0m,
            networkCost: 0m,
            otherCost: 0m,
            currency: "USD",
            costBreakdown: null,
            attributionMethod: null,
            dataSources: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compute_NegativeComputeCost_ShouldThrow()
    {
        var act = () => CostAttribution.Compute(
            dimension: CostAttributionDimension.Service,
            dimensionKey: "svc",
            dimensionLabel: null,
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            totalCost: 0m,
            computeCost: -5m,
            storageCost: 0m,
            networkCost: 0m,
            otherCost: 0m,
            currency: "USD",
            costBreakdown: null,
            attributionMethod: null,
            dataSources: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Guard clauses ──

    [Fact]
    public void Compute_NullDimensionKey_ShouldThrow()
    {
        var act = () => CostAttribution.Compute(
            dimension: CostAttributionDimension.Service,
            dimensionKey: null!,
            dimensionLabel: null,
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            totalCost: 0m,
            computeCost: 0m,
            storageCost: 0m,
            networkCost: 0m,
            otherCost: 0m,
            currency: "USD",
            costBreakdown: null,
            attributionMethod: null,
            dataSources: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compute_EmptyDimensionKey_ShouldThrow()
    {
        var act = () => CostAttribution.Compute(
            dimension: CostAttributionDimension.Service,
            dimensionKey: "   ",
            dimensionLabel: null,
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            totalCost: 0m,
            computeCost: 0m,
            storageCost: 0m,
            networkCost: 0m,
            otherCost: 0m,
            currency: "USD",
            costBreakdown: null,
            attributionMethod: null,
            dataSources: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compute_NullCurrency_ShouldThrow()
    {
        var act = () => CostAttribution.Compute(
            dimension: CostAttributionDimension.Service,
            dimensionKey: "svc",
            dimensionLabel: null,
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            totalCost: 0m,
            computeCost: 0m,
            storageCost: 0m,
            networkCost: 0m,
            otherCost: 0m,
            currency: null!,
            costBreakdown: null,
            attributionMethod: null,
            dataSources: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compute_PeriodEndBeforePeriodStart_ShouldThrow()
    {
        var act = () => CostAttribution.Compute(
            dimension: CostAttributionDimension.Service,
            dimensionKey: "svc",
            dimensionLabel: null,
            periodStart: PeriodEnd,
            periodEnd: PeriodStart,
            totalCost: 0m,
            computeCost: 0m,
            storageCost: 0m,
            networkCost: 0m,
            otherCost: 0m,
            currency: "USD",
            costBreakdown: null,
            attributionMethod: null,
            dataSources: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compute_DimensionKeyTooLong_ShouldThrow()
    {
        var act = () => CostAttribution.Compute(
            dimension: CostAttributionDimension.Service,
            dimensionKey: new string('x', 201),
            dimensionLabel: null,
            periodStart: PeriodStart,
            periodEnd: PeriodEnd,
            totalCost: 0m,
            computeCost: 0m,
            storageCost: 0m,
            networkCost: 0m,
            otherCost: 0m,
            currency: "USD",
            costBreakdown: null,
            attributionMethod: null,
            dataSources: null,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Helper ──

    private static CostAttribution CreateValid() => CostAttribution.Compute(
        dimension: CostAttributionDimension.Service,
        dimensionKey: "payment-service",
        dimensionLabel: "Payment Service",
        periodStart: PeriodStart,
        periodEnd: PeriodEnd,
        totalCost: 1000m,
        computeCost: 500m,
        storageCost: 200m,
        networkCost: 100m,
        otherCost: 200m,
        currency: "USD",
        costBreakdown: """{"compute_detail":"2 vCPU instances"}""",
        attributionMethod: "telemetry-based",
        dataSources: """["otel-collector","billing-api"]""",
        tenantId: "tenant1",
        now: FixedNow);
}
