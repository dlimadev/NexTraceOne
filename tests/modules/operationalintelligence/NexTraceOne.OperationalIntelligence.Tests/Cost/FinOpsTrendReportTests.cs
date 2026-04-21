using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetFinOpsTrendReport;
using NexTraceOne.OperationalIntelligence.Domain.FinOps.Entities;
using NexTraceOne.OperationalIntelligence.Domain.FinOps.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Cost;

/// <summary>
/// Testes unitários para Wave O.2 — GetFinOpsTrendReport.
/// Cobre série temporal de custo diário, distribuição por categoria,
/// ranking de serviços, delta período-a-período e comportamento com dados vazios.
/// </summary>
public sealed class FinOpsTrendReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-finops-trend";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static ServiceCostAllocationRecord MakeCostRecord(
        string serviceName,
        decimal amount,
        CostCategory category = CostCategory.Compute,
        int daysAgo = 1,
        string? teamId = null)
    {
        var periodStart = FixedNow.AddDays(-daysAgo);
        var periodEnd = periodStart.AddHours(23);
        return ServiceCostAllocationRecord.Create(
            tenantId: TenantId,
            serviceName: serviceName,
            environment: "production",
            category: category,
            amountUsd: amount,
            periodStart: periodStart,
            periodEnd: periodEnd,
            createdAt: FixedNow,
            teamId: teamId);
    }

    private static IServiceCostAllocationRepository MakeRepo(
        IReadOnlyList<ServiceCostAllocationRecord> currentRecords,
        IReadOnlyList<ServiceCostAllocationRecord>? previousRecords = null)
    {
        var repo = Substitute.For<IServiceCostAllocationRepository>();
        // Match current period (FixedNow - 30d to FixedNow)
        repo.ListByTenantAsync(
            TenantId,
            Arg.Is<DateTimeOffset>(d => d >= FixedNow.AddDays(-31)),
            FixedNow,
            Arg.Any<string?>(),
            Arg.Any<CostCategory?>(),
            Arg.Any<CancellationToken>())
            .Returns(currentRecords);

        // Match previous period
        repo.ListByTenantAsync(
            TenantId,
            Arg.Is<DateTimeOffset>(d => d < FixedNow.AddDays(-29)),
            Arg.Is<DateTimeOffset>(d => d < FixedNow),
            Arg.Any<string?>(),
            Arg.Any<CostCategory?>(),
            Arg.Any<CancellationToken>())
            .Returns(previousRecords ?? []);

        return repo;
    }

    // ── Empty report ──────────────────────────────────────────────────────

    [Fact]
    public async Task Report_IsSuccess_When_NoRecords()
    {
        var repo = Substitute.For<IServiceCostAllocationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetFinOpsTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetFinOpsTrendReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentPeriodTotalUsd.Should().Be(0m);
        result.Value.PreviousPeriodTotalUsd.Should().Be(0m);
        result.Value.DeltaPercent.Should().Be(0m);
        result.Value.DailySeries.Should().BeEmpty();
        result.Value.ByCategory.Should().BeEmpty();
        result.Value.TopServices.Should().BeEmpty();
    }

    // ── Total and delta calculation ───────────────────────────────────────

    [Fact]
    public async Task CurrentTotal_Sums_All_Records()
    {
        var records = new[]
        {
            MakeCostRecord("svc-a", 100m),
            MakeCostRecord("svc-b", 200m),
            MakeCostRecord("svc-a", 50m),
        };
        var repo = Substitute.For<IServiceCostAllocationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>())
            .Returns(records);

        var handler = new GetFinOpsTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetFinOpsTrendReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentPeriodTotalUsd.Should().Be(350m);
    }

    [Fact]
    public async Task DeltaPercent_PositiveGrowth_When_CurrentHigherThanPrevious()
    {
        var currentRecords = new[] { MakeCostRecord("svc-a", 150m) };
        var previousRecords = new[] { MakeCostRecord("svc-a", 100m) };

        var repo = Substitute.For<IServiceCostAllocationRepository>();
        // First call = current period, second = previous
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>())
            .Returns(currentRecords, previousRecords);

        var handler = new GetFinOpsTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetFinOpsTrendReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentPeriodTotalUsd.Should().Be(150m);
        result.Value.PreviousPeriodTotalUsd.Should().Be(100m);
        result.Value.DeltaPercent.Should().Be(50m);
    }

    [Fact]
    public async Task DeltaPercent_100_When_PreviousIsZero_And_CurrentPositive()
    {
        var currentRecords = new[] { MakeCostRecord("svc-a", 200m) };

        var repo = Substitute.For<IServiceCostAllocationRepository>();
        // First call = current period, second = previous (empty)
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>())
            .Returns(
                currentRecords,
                (IReadOnlyList<ServiceCostAllocationRecord>)Array.Empty<ServiceCostAllocationRecord>());

        var handler = new GetFinOpsTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetFinOpsTrendReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DeltaPercent.Should().Be(100m);
    }

    [Fact]
    public async Task DeltaPercent_Zero_When_BothZero()
    {
        var repo = Substitute.For<IServiceCostAllocationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetFinOpsTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetFinOpsTrendReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DeltaPercent.Should().Be(0m);
    }

    // ── Daily series ──────────────────────────────────────────────────────

    [Fact]
    public async Task DailySeries_GroupsRecords_ByDay()
    {
        var records = new[]
        {
            MakeCostRecord("svc-a", 100m, daysAgo: 5),
            MakeCostRecord("svc-b", 50m, daysAgo: 5),
            MakeCostRecord("svc-a", 200m, daysAgo: 3),
        };
        var repo = Substitute.For<IServiceCostAllocationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>())
            .Returns(records, []);

        var handler = new GetFinOpsTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetFinOpsTrendReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DailySeries.Count.Should().Be(2);

        var day5 = result.Value.DailySeries.FirstOrDefault(d => d.TotalUsd == 150m);
        day5.Should().NotBeNull();

        var day3 = result.Value.DailySeries.FirstOrDefault(d => d.TotalUsd == 200m);
        day3.Should().NotBeNull();
    }

    [Fact]
    public async Task DailySeries_IsOrderedByDate_Ascending()
    {
        var records = new[]
        {
            MakeCostRecord("svc-a", 100m, daysAgo: 3),
            MakeCostRecord("svc-b", 50m, daysAgo: 10),
            MakeCostRecord("svc-c", 25m, daysAgo: 1),
        };
        var repo = Substitute.For<IServiceCostAllocationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>())
            .Returns(records, []);

        var handler = new GetFinOpsTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetFinOpsTrendReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dates = result.Value.DailySeries.Select(d => d.Date).ToList();
        dates.Should().BeInAscendingOrder();
    }

    // ── Category breakdown ────────────────────────────────────────────────

    [Fact]
    public async Task ByCategory_Correct_Distribution()
    {
        var records = new[]
        {
            MakeCostRecord("svc-a", 200m, CostCategory.Compute),
            MakeCostRecord("svc-b", 100m, CostCategory.Storage),
            MakeCostRecord("svc-c", 100m, CostCategory.Network),
        };
        var repo = Substitute.For<IServiceCostAllocationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>())
            .Returns(records, []);

        var handler = new GetFinOpsTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetFinOpsTrendReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var compute = result.Value.ByCategory.FirstOrDefault(c => c.Category == "Compute");
        compute.Should().NotBeNull();
        compute!.TotalUsd.Should().Be(200m);
        compute.Percent.Should().Be(50m);
    }

    [Fact]
    public async Task ByCategory_OrderedByTotalDesc()
    {
        var records = new[]
        {
            MakeCostRecord("svc-a", 10m, CostCategory.Network),
            MakeCostRecord("svc-b", 300m, CostCategory.Compute),
            MakeCostRecord("svc-c", 100m, CostCategory.Storage),
        };
        var repo = Substitute.For<IServiceCostAllocationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>())
            .Returns(records, []);

        var handler = new GetFinOpsTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetFinOpsTrendReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByCategory.First().Category.Should().Be("Compute");
    }

    // ── Top services ──────────────────────────────────────────────────────

    [Fact]
    public async Task TopServices_LimitedByCount()
    {
        var records = Enumerable.Range(1, 20)
            .Select(i => MakeCostRecord($"svc-{i}", i * 10m))
            .ToList();
        var repo = Substitute.For<IServiceCostAllocationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>())
            .Returns(records, []);

        var handler = new GetFinOpsTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetFinOpsTrendReport.Query(TenantId, TopServicesCount: 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopServices.Count.Should().Be(5);
    }

    [Fact]
    public async Task TopServices_OrderedByCostDesc()
    {
        var records = new[]
        {
            MakeCostRecord("cheap-svc", 10m),
            MakeCostRecord("expensive-svc", 1000m),
            MakeCostRecord("mid-svc", 300m),
        };
        var repo = Substitute.For<IServiceCostAllocationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>())
            .Returns(records, []);

        var handler = new GetFinOpsTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetFinOpsTrendReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopServices.First().ServiceName.Should().Be("expensive-svc");
    }

    [Fact]
    public async Task TopServices_TeamId_Preserved()
    {
        var records = new[]
        {
            MakeCostRecord("svc-a", 500m, teamId: "team-payments"),
        };
        var repo = Substitute.For<IServiceCostAllocationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>())
            .Returns(records, []);

        var handler = new GetFinOpsTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetFinOpsTrendReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopServices.First().TeamId.Should().Be("team-payments");
    }

    // ── Report metadata ───────────────────────────────────────────────────

    [Fact]
    public async Task Report_Metadata_Correct()
    {
        var repo = Substitute.For<IServiceCostAllocationRepository>();
        repo.ListByTenantAsync(TenantId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), Arg.Any<CostCategory?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetFinOpsTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetFinOpsTrendReport.Query(TenantId, LookbackDays: 14), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(TenantId);
        result.Value.LookbackDays.Should().Be(14);
        result.Value.GeneratedAt.Should().Be(FixedNow);
        result.Value.From.Should().BeCloseTo(FixedNow.AddDays(-14), TimeSpan.FromSeconds(1));
        result.Value.To.Should().Be(FixedNow);
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_Rejects_EmptyTenantId()
    {
        var validator = new GetFinOpsTrendReport.Validator();
        var result = validator.Validate(new GetFinOpsTrendReport.Query(""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Rejects_LookbackDays_Zero()
    {
        var validator = new GetFinOpsTrendReport.Validator();
        var result = validator.Validate(new GetFinOpsTrendReport.Query(TenantId, LookbackDays: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Rejects_TopServicesCount_Zero()
    {
        var validator = new GetFinOpsTrendReport.Validator();
        var result = validator.Validate(new GetFinOpsTrendReport.Query(TenantId, TopServicesCount: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Accepts_Valid_Query()
    {
        var validator = new GetFinOpsTrendReport.Validator();
        var result = validator.Validate(new GetFinOpsTrendReport.Query(TenantId, LookbackDays: 30, TopServicesCount: 10));
        result.IsValid.Should().BeTrue();
    }
}
