using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetFinOpsInsights;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetServiceCostAllocationReport;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Features.IngestServiceCostRecord;
using NexTraceOne.OperationalIntelligence.Domain.FinOps.Entities;
using NexTraceOne.OperationalIntelligence.Domain.FinOps.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Cost;

/// <summary>
/// Testes unitários para Wave I.2 — FinOps Contextual por Serviço.
/// Cobre entidade ServiceCostAllocationRecord, IngestServiceCostRecord,
/// GetServiceCostAllocationReport e GetFinOpsInsights.
/// </summary>
public sealed class ServiceCostAllocationTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodStart = new(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodEnd = new(2026, 4, 30, 23, 59, 59, TimeSpan.Zero);

    private readonly IServiceCostAllocationRepository _repository = Substitute.For<IServiceCostAllocationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public ServiceCostAllocationTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    // ── Domain: ServiceCostAllocationRecord entity ────────────────────────

    [Fact]
    public void ServiceCostAllocationRecord_Create_Succeeds_With_Valid_Input()
    {
        var record = ServiceCostAllocationRecord.Create(
            "tenant-1", "payment-service", "production",
            CostCategory.Compute, 150.50m, PeriodStart, PeriodEnd, FixedNow);

        record.Should().NotBeNull();
        record.TenantId.Should().Be("tenant-1");
        record.ServiceName.Should().Be("payment-service");
        record.Environment.Should().Be("production");
        record.Category.Should().Be(CostCategory.Compute);
        record.AmountUsd.Should().Be(150.50m);
        record.Currency.Should().Be("USD");
        record.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void ServiceCostAllocationRecord_Create_Throws_When_PeriodEnd_Before_PeriodStart()
    {
        var act = () => ServiceCostAllocationRecord.Create(
            "tenant-1", "svc", "prod",
            CostCategory.Storage, 10m, PeriodEnd, PeriodStart, FixedNow);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void ServiceCostAllocationRecord_Create_Throws_On_Negative_Amount()
    {
        var act = () => ServiceCostAllocationRecord.Create(
            "tenant-1", "svc", "prod",
            CostCategory.Network, -1m, PeriodStart, PeriodEnd, FixedNow);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void ServiceCostAllocationRecord_Create_Accepts_Optional_Fields()
    {
        var record = ServiceCostAllocationRecord.Create(
            "tenant-1", "svc", "staging",
            CostCategory.Licensing, 25m, PeriodStart, PeriodEnd, FixedNow,
            teamId: "team-alpha", domainName: "finance",
            currency: "EUR", originalAmount: 23m,
            tagsJson: "{\"env\":\"staging\"}", source: "azure");

        record.TeamId.Should().Be("team-alpha");
        record.DomainName.Should().Be("finance");
        record.Currency.Should().Be("EUR");
        record.OriginalAmount.Should().Be(23m);
        record.Source.Should().Be("azure");
    }

    // ── IngestServiceCostRecord handler ─────────────────────────────────

    [Fact]
    public async Task IngestServiceCostRecord_Handler_Creates_And_Returns_Record()
    {
        var handler = new IngestServiceCostRecord.Handler(_repository, _unitOfWork, _clock);
        var cmd = new IngestServiceCostRecord.Command(
            "tenant-1", "api-gw", "production",
            CostCategory.Compute, 200m, PeriodStart, PeriodEnd);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("api-gw");
        result.Value.Category.Should().Be(CostCategory.Compute);
        result.Value.AmountUsd.Should().Be(200m);
        _repository.Received(1).Add(Arg.Any<ServiceCostAllocationRecord>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestServiceCostRecord_Validator_Rejects_Empty_ServiceName()
    {
        var validator = new IngestServiceCostRecord.Validator();
        var result = validator.Validate(new IngestServiceCostRecord.Command(
            "tenant-1", "", "prod", CostCategory.Compute, 10m, PeriodStart, PeriodEnd));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task IngestServiceCostRecord_Validator_Rejects_Negative_Amount()
    {
        var validator = new IngestServiceCostRecord.Validator();
        var result = validator.Validate(new IngestServiceCostRecord.Command(
            "tenant-1", "svc", "prod", CostCategory.Compute, -5m, PeriodStart, PeriodEnd));

        result.IsValid.Should().BeFalse();
    }

    // ── GetServiceCostAllocationReport handler ───────────────────────────

    [Fact]
    public async Task GetServiceCostAllocationReport_Returns_Grouped_By_Service()
    {
        var records = new List<ServiceCostAllocationRecord>
        {
            ServiceCostAllocationRecord.Create("t1", "svc-a", "prod", CostCategory.Compute, 100m, PeriodStart, PeriodEnd, FixedNow),
            ServiceCostAllocationRecord.Create("t1", "svc-a", "prod", CostCategory.Storage, 50m, PeriodStart, PeriodEnd, FixedNow),
            ServiceCostAllocationRecord.Create("t1", "svc-b", "prod", CostCategory.Network, 200m, PeriodStart, PeriodEnd, FixedNow),
        };
        _repository.ListByTenantAsync("t1", Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ServiceCostAllocationRecord>)records);

        var handler = new GetServiceCostAllocationReport.Handler(_repository, _clock);
        var result = await handler.Handle(new GetServiceCostAllocationReport.Query("t1", 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Services.Should().HaveCount(2);
        result.Value.GrandTotalUsd.Should().Be(350m);
        var svcA = result.Value.Services.First(s => s.ServiceName == "svc-a");
        svcA.TotalAmountUsd.Should().Be(150m);
        svcA.ByCategory.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetServiceCostAllocationReport_Empty_When_No_Records()
    {
        _repository.ListByTenantAsync("t1", Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ServiceCostAllocationRecord>)[]);

        var handler = new GetServiceCostAllocationReport.Handler(_repository, _clock);
        var result = await handler.Handle(new GetServiceCostAllocationReport.Query("t1", 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GrandTotalUsd.Should().Be(0m);
        result.Value.Services.Should().BeEmpty();
    }

    // ── GetFinOpsInsights handler ────────────────────────────────────────

    [Fact]
    public async Task GetFinOpsInsights_Detects_CostOutlier_Above_P75()
    {
        // 4 services: P75 threshold = sorted[ceil(4*0.75)-1] = sorted[2] = 50
        // expensive-svc (5000) > 50 → outlier detected
        var records = new List<ServiceCostAllocationRecord>
        {
            ServiceCostAllocationRecord.Create("t1", "cheap-svc", "prod", CostCategory.Compute, 5m, PeriodStart, PeriodEnd, FixedNow),
            ServiceCostAllocationRecord.Create("t1", "medium-svc", "prod", CostCategory.Compute, 20m, PeriodStart, PeriodEnd, FixedNow),
            ServiceCostAllocationRecord.Create("t1", "above-avg-svc", "prod", CostCategory.Compute, 50m, PeriodStart, PeriodEnd, FixedNow),
            ServiceCostAllocationRecord.Create("t1", "expensive-svc", "prod", CostCategory.Compute, 5000m, PeriodStart, PeriodEnd, FixedNow),
        };
        // Both current and previous period calls
        _repository.ListByTenantAsync("t1", Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ServiceCostAllocationRecord>)records, (IReadOnlyList<ServiceCostAllocationRecord>)[]);

        var handler = new GetFinOpsInsights.Handler(_repository, _clock);
        var result = await handler.Handle(new GetFinOpsInsights.Query("t1", 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Insights.Should().Contain(i => i.InsightType == GetFinOpsInsights.FinOpsInsightType.CostOutlier);
    }

    [Fact]
    public async Task GetFinOpsInsights_Returns_Empty_Insights_When_No_Data()
    {
        _repository.ListByTenantAsync("t1", Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ServiceCostAllocationRecord>)[]);

        var handler = new GetFinOpsInsights.Handler(_repository, _clock);
        var result = await handler.Handle(new GetFinOpsInsights.Query("t1", 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Insights.Should().BeEmpty();
        result.Value.TotalInsights.Should().Be(0);
    }

    [Fact]
    public void CostCategory_Enum_Has_Expected_Values()
    {
        var values = Enum.GetValues<CostCategory>();
        values.Should().Contain([
            CostCategory.Compute,
            CostCategory.Storage,
            CostCategory.Network,
            CostCategory.Licensing,
            CostCategory.Observability,
            CostCategory.Other,
        ]);
    }
}
