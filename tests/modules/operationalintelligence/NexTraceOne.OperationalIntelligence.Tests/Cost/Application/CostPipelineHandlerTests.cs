using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.ComputeCostTrend;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.CreateServiceCostProfile;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostRecordsByService;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.ListCostImportBatches;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;
using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Cost.Application;

/// <summary>
/// Testes unitários dos novos handlers introduzidos na P6.3:
/// ComputeCostTrend (persistence fix), CreateServiceCostProfile,
/// ListCostImportBatches e GetCostRecordsByService.
/// </summary>
public sealed class CostPipelineHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider MockClock()
    {
        var c = Substitute.For<IDateTimeProvider>();
        c.UtcNow.Returns(FixedNow);
        return c;
    }

    // ── ComputeCostTrend — persistence fix ─────────────────────────────────

    [Fact]
    public async Task ComputeCostTrend_WithSnapshots_ShouldPersistTrend()
    {
        var snapshot = CostSnapshot.Create(
            "svc-api", "production", 100m, 30m, 20m, 15m, 10m,
            FixedNow.AddDays(-5), "AWS CUR", "2026-03", "USD").Value;

        var snapshot2 = CostSnapshot.Create(
            "svc-api", "production", 120m, 36m, 24m, 18m, 12m,
            FixedNow.AddDays(-2), "AWS CUR", "2026-03", "USD").Value;

        var snapshotRepo = Substitute.For<ICostSnapshotRepository>();
        snapshotRepo.ListByServiceAsync("svc-api", "production", 1, 1000, Arg.Any<CancellationToken>())
            .Returns(new List<CostSnapshot> { snapshot, snapshot2 });

        var trendRepo = Substitute.For<ICostTrendRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ComputeCostTrend.Handler(snapshotRepo, trendRepo, uow);

        var command = new ComputeCostTrend.Command(
            "svc-api", "production",
            FixedNow.AddDays(-7),
            FixedNow);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("svc-api");
        result.Value.DataPointCount.Should().Be(2);
        trendRepo.Received(1).Add(Arg.Any<CostTrend>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ComputeCostTrend_WithNoSnapshots_ShouldStillPersistWithZeroCost()
    {
        var snapshotRepo = Substitute.For<ICostSnapshotRepository>();
        snapshotRepo.ListByServiceAsync(Arg.Any<string>(), Arg.Any<string>(), 1, 1000, Arg.Any<CancellationToken>())
            .Returns(new List<CostSnapshot>());

        var trendRepo = Substitute.For<ICostTrendRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ComputeCostTrend.Handler(snapshotRepo, trendRepo, uow);

        var command = new ComputeCostTrend.Command(
            "svc-empty", "production",
            FixedNow.AddDays(-7),
            FixedNow);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AverageDailyCost.Should().Be(0m);
        trendRepo.Received(1).Add(Arg.Any<CostTrend>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── CreateServiceCostProfile ─────────────────────────────────────────────

    [Fact]
    public async Task CreateServiceCostProfile_WithNoExistingProfile_ShouldCreateAndPersist()
    {
        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        profileRepo.GetByServiceAndEnvironmentAsync("svc-api", "production", Arg.Any<CancellationToken>())
            .Returns((ServiceCostProfile?)null);

        var uow = Substitute.For<IUnitOfWork>();
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new CreateServiceCostProfile.Handler(profileRepo, uow, MockClock());

        var result = await handler.Handle(
            new CreateServiceCostProfile.Command("svc-api", "production", 80m, 500m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("svc-api");
        result.Value.Environment.Should().Be("production");
        result.Value.AlertThresholdPercent.Should().Be(80m);
        result.Value.MonthlyBudget.Should().Be(500m);
        result.Value.CurrentMonthCost.Should().Be(0m);
        result.Value.IsOverBudget.Should().BeFalse();

        profileRepo.Received(1).Add(Arg.Any<ServiceCostProfile>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateServiceCostProfile_WithExistingProfile_ShouldReturnExistingIdempotently()
    {
        var existing = ServiceCostProfile.Create("svc-api", "production", 75m, FixedNow, 1000m);

        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        profileRepo.GetByServiceAndEnvironmentAsync("svc-api", "production", Arg.Any<CancellationToken>())
            .Returns(existing);

        var uow = Substitute.For<IUnitOfWork>();

        var handler = new CreateServiceCostProfile.Handler(profileRepo, uow, MockClock());

        var result = await handler.Handle(
            new CreateServiceCostProfile.Command("svc-api", "production"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProfileId.Should().Be(existing.Id.Value);
        result.Value.AlertThresholdPercent.Should().Be(75m);

        profileRepo.DidNotReceive().Add(Arg.Any<ServiceCostProfile>());
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateServiceCostProfile_WithoutBudget_ShouldCreateWithNullBudget()
    {
        var profileRepo = Substitute.For<IServiceCostProfileRepository>();
        profileRepo.GetByServiceAndEnvironmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ServiceCostProfile?)null);

        var uow = Substitute.For<IUnitOfWork>();
        uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new CreateServiceCostProfile.Handler(profileRepo, uow, MockClock());

        var result = await handler.Handle(
            new CreateServiceCostProfile.Command("svc-api", "staging", 80m, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MonthlyBudget.Should().BeNull();
        result.Value.BudgetUsagePercent.Should().BeNull();
    }

    // ── ListCostImportBatches ────────────────────────────────────────────────

    [Fact]
    public async Task ListCostImportBatches_ShouldReturnPagedBatches()
    {
        var batch1 = CostImportBatch.Create("AWS CUR", "2026-03", FixedNow, "USD").Value;
        batch1.Complete(10);
        var batch2 = CostImportBatch.Create("Azure", "2026-02", FixedNow.AddDays(-30), "EUR").Value;
        batch2.Complete(5);

        var batchRepo = Substitute.For<ICostImportBatchRepository>();
        batchRepo.ListAsync(1, 20, Arg.Any<CancellationToken>())
            .Returns(new List<CostImportBatch> { batch1, batch2 });

        var handler = new ListCostImportBatches.Handler(batchRepo);

        var result = await handler.Handle(new ListCostImportBatches.Query(1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Source.Should().Be("AWS CUR");
        result.Value.Items[0].RecordCount.Should().Be(10);
        result.Value.Items[0].Status.Should().Be("Completed");
    }

    [Fact]
    public async Task ListCostImportBatches_WithEmptyList_ShouldReturnEmpty()
    {
        var batchRepo = Substitute.For<ICostImportBatchRepository>();
        batchRepo.ListAsync(1, 20, Arg.Any<CancellationToken>())
            .Returns(new List<CostImportBatch>());

        var handler = new ListCostImportBatches.Handler(batchRepo);

        var result = await handler.Handle(new ListCostImportBatches.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    // ── GetCostRecordsByService ──────────────────────────────────────────────

    [Fact]
    public async Task GetCostRecordsByService_WithRecords_ShouldReturnTotalsAndItems()
    {
        var batchId = Guid.NewGuid();
        var r1 = CostRecord.Create(batchId, "svc-api", "API Service", "team-a", "commerce", "production", "2026-03", 200m, "USD", "AWS CUR", FixedNow).Value;
        var r2 = CostRecord.Create(batchId, "svc-api", "API Service", "team-a", "commerce", "production", "2026-03", 150m, "USD", "AWS CUR", FixedNow.AddHours(-1)).Value;

        var recordRepo = Substitute.For<ICostRecordRepository>();
        recordRepo.ListByServiceAsync("svc-api", "2026-03", Arg.Any<CancellationToken>())
            .Returns(new List<CostRecord> { r1, r2 });

        var handler = new GetCostRecordsByService.Handler(recordRepo);

        var result = await handler.Handle(
            new GetCostRecordsByService.Query("svc-api", "2026-03"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceId.Should().Be("svc-api");
        result.Value.TotalCost.Should().Be(350m);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCostRecordsByService_WithNoPeriodFilter_ShouldReturnAllRecords()
    {
        var batchId = Guid.NewGuid();
        var record = CostRecord.Create(batchId, "svc-api", "API Service", null, null, null, "2026-03", 100m, "USD", "AWS CUR", FixedNow).Value;

        var recordRepo = Substitute.For<ICostRecordRepository>();
        recordRepo.ListByServiceAsync("svc-api", null, Arg.Any<CancellationToken>())
            .Returns(new List<CostRecord> { record });

        var handler = new GetCostRecordsByService.Handler(recordRepo);

        var result = await handler.Handle(
            new GetCostRecordsByService.Query("svc-api"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCost.Should().Be(100m);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCostRecordsByService_WithNoRecords_ShouldReturnZeroTotal()
    {
        var recordRepo = Substitute.For<ICostRecordRepository>();
        recordRepo.ListByServiceAsync("svc-unknown", null, Arg.Any<CancellationToken>())
            .Returns(new List<CostRecord>());

        var handler = new GetCostRecordsByService.Handler(recordRepo);

        var result = await handler.Handle(
            new GetCostRecordsByService.Query("svc-unknown"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCost.Should().Be(0m);
        result.Value.Items.Should().BeEmpty();
    }
}
