using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Domain;

/// <summary>
/// Testes unitários para a entidade ChangeCostImpact.
/// Valida cálculo de delta, percentagem, direção e guard clauses.
/// </summary>
public sealed class ChangeCostImpactTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WindowStart = new(2026, 6, 10, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WindowEnd = new(2026, 6, 14, 0, 0, 0, TimeSpan.Zero);

    // ── Direction derivation ──

    [Fact]
    public void Record_CostIncrease_ShouldSetDirectionIncrease()
    {
        var impact = CreateImpact(baseline: 100m, actual: 123m);

        impact.Direction.Should().Be(CostChangeDirection.Increase);
        impact.CostDelta.Should().Be(23m);
        impact.CostDeltaPercentage.Should().Be(23m);
    }

    [Fact]
    public void Record_CostDecrease_ShouldSetDirectionDecrease()
    {
        var impact = CreateImpact(baseline: 200m, actual: 150m);

        impact.Direction.Should().Be(CostChangeDirection.Decrease);
        impact.CostDelta.Should().Be(-50m);
        impact.CostDeltaPercentage.Should().Be(-25m);
    }

    [Fact]
    public void Record_NoCostChange_ShouldSetDirectionNeutral()
    {
        var impact = CreateImpact(baseline: 100m, actual: 100m);

        impact.Direction.Should().Be(CostChangeDirection.Neutral);
        impact.CostDelta.Should().Be(0m);
        impact.CostDeltaPercentage.Should().Be(0m);
    }

    // ── Delta and percentage calculations ──

    [Fact]
    public void Record_ShouldCalculateDeltaCorrectly()
    {
        var impact = CreateImpact(baseline: 80m, actual: 100m);

        impact.CostDelta.Should().Be(20m);
        impact.CostDeltaPercentage.Should().Be(25m);
    }

    [Fact]
    public void Record_ZeroBaseline_PositiveActual_ShouldReturn100Percent()
    {
        var impact = CreateImpact(baseline: 0m, actual: 50m);

        impact.CostDelta.Should().Be(50m);
        impact.CostDeltaPercentage.Should().Be(100m);
        impact.Direction.Should().Be(CostChangeDirection.Increase);
    }

    [Fact]
    public void Record_ZeroBaseline_ZeroActual_ShouldReturnNeutral()
    {
        var impact = CreateImpact(baseline: 0m, actual: 0m);

        impact.CostDelta.Should().Be(0m);
        impact.CostDeltaPercentage.Should().Be(0m);
        impact.Direction.Should().Be(CostChangeDirection.Neutral);
    }

    [Fact]
    public void Record_LargePercentageIncrease_ShouldCalculateCorrectly()
    {
        var impact = CreateImpact(baseline: 10m, actual: 33m);

        impact.CostDelta.Should().Be(23m);
        impact.CostDeltaPercentage.Should().Be(230m);
        impact.Direction.Should().Be(CostChangeDirection.Increase);
    }

    // ── Properties set correctly ──

    [Fact]
    public void Record_ShouldSetAllPropertiesCorrectly()
    {
        var releaseId = Guid.NewGuid();
        var impact = ChangeCostImpact.Record(
            releaseId: releaseId,
            serviceName: "  order-service  ",
            environment: "  production  ",
            changeDescription: "  Add caching layer  ",
            baselineCostPerDay: 100m,
            actualCostPerDay: 80m,
            costProvider: "  AWS  ",
            costDetails: """{"compute": 60, "storage": 20}""",
            measurementWindowStart: WindowStart,
            measurementWindowEnd: WindowEnd,
            tenantId: "  tenant-abc  ",
            now: FixedNow);

        impact.Id.Value.Should().NotBe(Guid.Empty);
        impact.ReleaseId.Should().Be(releaseId);
        impact.ServiceName.Should().Be("order-service");
        impact.Environment.Should().Be("production");
        impact.ChangeDescription.Should().Be("Add caching layer");
        impact.BaselineCostPerDay.Should().Be(100m);
        impact.ActualCostPerDay.Should().Be(80m);
        impact.CostProvider.Should().Be("AWS");
        impact.CostDetails.Should().Be("""{"compute": 60, "storage": 20}""");
        impact.MeasurementWindowStart.Should().Be(WindowStart);
        impact.MeasurementWindowEnd.Should().Be(WindowEnd);
        impact.RecordedAt.Should().Be(FixedNow);
        impact.TenantId.Should().Be("tenant-abc");
        impact.Direction.Should().Be(CostChangeDirection.Decrease);
    }

    [Fact]
    public void Record_NullOptionalFields_ShouldSucceed()
    {
        var impact = ChangeCostImpact.Record(
            releaseId: Guid.NewGuid(),
            serviceName: "svc",
            environment: "dev",
            changeDescription: null,
            baselineCostPerDay: 50m,
            actualCostPerDay: 50m,
            costProvider: null,
            costDetails: null,
            measurementWindowStart: WindowStart,
            measurementWindowEnd: WindowEnd,
            tenantId: null,
            now: FixedNow);

        impact.ChangeDescription.Should().BeNull();
        impact.CostProvider.Should().BeNull();
        impact.CostDetails.Should().BeNull();
        impact.TenantId.Should().BeNull();
    }

    // ── Guard clauses ──

    [Fact]
    public void Record_DefaultReleaseId_ShouldThrow()
    {
        var act = () => ChangeCostImpact.Record(
            releaseId: Guid.Empty,
            serviceName: "svc",
            environment: "prod",
            changeDescription: null,
            baselineCostPerDay: 100m,
            actualCostPerDay: 120m,
            costProvider: null,
            costDetails: null,
            measurementWindowStart: WindowStart,
            measurementWindowEnd: WindowEnd,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_NullServiceName_ShouldThrow()
    {
        var act = () => ChangeCostImpact.Record(
            releaseId: Guid.NewGuid(),
            serviceName: null!,
            environment: "prod",
            changeDescription: null,
            baselineCostPerDay: 100m,
            actualCostPerDay: 120m,
            costProvider: null,
            costDetails: null,
            measurementWindowStart: WindowStart,
            measurementWindowEnd: WindowEnd,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_EmptyServiceName_ShouldThrow()
    {
        var act = () => ChangeCostImpact.Record(
            releaseId: Guid.NewGuid(),
            serviceName: "   ",
            environment: "prod",
            changeDescription: null,
            baselineCostPerDay: 100m,
            actualCostPerDay: 120m,
            costProvider: null,
            costDetails: null,
            measurementWindowStart: WindowStart,
            measurementWindowEnd: WindowEnd,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_EmptyEnvironment_ShouldThrow()
    {
        var act = () => ChangeCostImpact.Record(
            releaseId: Guid.NewGuid(),
            serviceName: "svc",
            environment: "",
            changeDescription: null,
            baselineCostPerDay: 100m,
            actualCostPerDay: 120m,
            costProvider: null,
            costDetails: null,
            measurementWindowStart: WindowStart,
            measurementWindowEnd: WindowEnd,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_NegativeBaseline_ShouldThrow()
    {
        var act = () => ChangeCostImpact.Record(
            releaseId: Guid.NewGuid(),
            serviceName: "svc",
            environment: "prod",
            changeDescription: null,
            baselineCostPerDay: -1m,
            actualCostPerDay: 120m,
            costProvider: null,
            costDetails: null,
            measurementWindowStart: WindowStart,
            measurementWindowEnd: WindowEnd,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_NegativeActual_ShouldThrow()
    {
        var act = () => ChangeCostImpact.Record(
            releaseId: Guid.NewGuid(),
            serviceName: "svc",
            environment: "prod",
            changeDescription: null,
            baselineCostPerDay: 100m,
            actualCostPerDay: -5m,
            costProvider: null,
            costDetails: null,
            measurementWindowStart: WindowStart,
            measurementWindowEnd: WindowEnd,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_WindowEndBeforeStart_ShouldThrow()
    {
        var act = () => ChangeCostImpact.Record(
            releaseId: Guid.NewGuid(),
            serviceName: "svc",
            environment: "prod",
            changeDescription: null,
            baselineCostPerDay: 100m,
            actualCostPerDay: 120m,
            costProvider: null,
            costDetails: null,
            measurementWindowStart: WindowEnd,
            measurementWindowEnd: WindowStart,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_WindowEndEqualsStart_ShouldThrow()
    {
        var act = () => ChangeCostImpact.Record(
            releaseId: Guid.NewGuid(),
            serviceName: "svc",
            environment: "prod",
            changeDescription: null,
            baselineCostPerDay: 100m,
            actualCostPerDay: 120m,
            costProvider: null,
            costDetails: null,
            measurementWindowStart: WindowStart,
            measurementWindowEnd: WindowStart,
            tenantId: null,
            now: FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    // ── Helper ──

    private static ChangeCostImpact CreateImpact(decimal baseline, decimal actual) =>
        ChangeCostImpact.Record(
            releaseId: Guid.NewGuid(),
            serviceName: "test-service",
            environment: "production",
            changeDescription: "test change",
            baselineCostPerDay: baseline,
            actualCostPerDay: actual,
            costProvider: "AWS",
            costDetails: null,
            measurementWindowStart: WindowStart,
            measurementWindowEnd: WindowEnd,
            tenantId: null,
            now: FixedNow);
}
