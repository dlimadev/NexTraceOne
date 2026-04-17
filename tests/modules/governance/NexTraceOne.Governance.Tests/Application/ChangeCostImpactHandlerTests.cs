using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.GetChangeCostImpact;
using NexTraceOne.Governance.Application.Features.ListCostliestChanges;
using NexTraceOne.Governance.Application.Features.RecordChangeCostImpact;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Tests.Application;

/// <summary>
/// Testes dos handlers de impacto de custo por mudança (FinOps).
/// Cobre RecordChangeCostImpact, GetChangeCostImpact e ListCostliestChanges.
/// </summary>
public sealed class ChangeCostImpactHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WindowStart = new(2026, 6, 10, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WindowEnd = new(2026, 6, 14, 0, 0, 0, TimeSpan.Zero);

    private readonly IChangeCostImpactRepository _repository =
        Substitute.For<IChangeCostImpactRepository>();
    private readonly IGovernanceUnitOfWork _unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public ChangeCostImpactHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
    }

    // ── RecordChangeCostImpact ──

    [Fact]
    public async Task Record_ShouldCreateImpactWithIncrease()
    {
        var handler = new RecordChangeCostImpact.Handler(_repository, _unitOfWork, _clock);
        var command = new RecordChangeCostImpact.Command(
            ReleaseId: Guid.NewGuid(),
            ServiceName: "order-service",
            Environment: "production",
            ChangeDescription: "Add new payment gateway",
            BaselineCostPerDay: 100m,
            ActualCostPerDay: 123m,
            CostProvider: "AWS",
            CostDetails: null,
            MeasurementWindowStart: WindowStart,
            MeasurementWindowEnd: WindowEnd,
            TenantId: "tenant1");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CostDelta.Should().Be(23m);
        result.Value.CostDeltaPercentage.Should().Be(23m);
        result.Value.Direction.Should().Be(CostChangeDirection.Increase);
        result.Value.ServiceName.Should().Be("order-service");
        result.Value.Environment.Should().Be("production");
        result.Value.RecordedAt.Should().Be(FixedNow);

        await _repository.Received(1).AddAsync(Arg.Any<ChangeCostImpact>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Record_ShouldCreateImpactWithDecrease()
    {
        var handler = new RecordChangeCostImpact.Handler(_repository, _unitOfWork, _clock);
        var command = new RecordChangeCostImpact.Command(
            ReleaseId: Guid.NewGuid(),
            ServiceName: "cache-service",
            Environment: "staging",
            ChangeDescription: "Optimize queries",
            BaselineCostPerDay: 200m,
            ActualCostPerDay: 150m,
            CostProvider: "Azure",
            CostDetails: null,
            MeasurementWindowStart: WindowStart,
            MeasurementWindowEnd: WindowEnd);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CostDelta.Should().Be(-50m);
        result.Value.Direction.Should().Be(CostChangeDirection.Decrease);
    }

    [Fact]
    public async Task Record_NeutralCost_ShouldReturnNeutralDirection()
    {
        var handler = new RecordChangeCostImpact.Handler(_repository, _unitOfWork, _clock);
        var command = new RecordChangeCostImpact.Command(
            ReleaseId: Guid.NewGuid(),
            ServiceName: "stable-service",
            Environment: "production",
            ChangeDescription: null,
            BaselineCostPerDay: 100m,
            ActualCostPerDay: 100m,
            CostProvider: null,
            CostDetails: null,
            MeasurementWindowStart: WindowStart,
            MeasurementWindowEnd: WindowEnd);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CostDelta.Should().Be(0m);
        result.Value.Direction.Should().Be(CostChangeDirection.Neutral);
    }

    // ── GetChangeCostImpact ──

    [Fact]
    public async Task Get_ExistingRelease_ShouldReturnImpact()
    {
        var releaseId = Guid.NewGuid();
        var impact = ChangeCostImpact.Record(
            releaseId: releaseId,
            serviceName: "api-svc",
            environment: "production",
            changeDescription: "Deploy v2.1",
            baselineCostPerDay: 100m,
            actualCostPerDay: 130m,
            costProvider: "AWS",
            costDetails: null,
            measurementWindowStart: WindowStart,
            measurementWindowEnd: WindowEnd,
            tenantId: "t1",
            now: FixedNow);

        _repository.GetByReleaseIdAsync(releaseId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ChangeCostImpact?>(impact));

        var handler = new GetChangeCostImpact.Handler(_repository);
        var result = await handler.Handle(new GetChangeCostImpact.Query(releaseId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(releaseId);
        result.Value.ServiceName.Should().Be("api-svc");
        result.Value.CostDelta.Should().Be(30m);
        result.Value.Direction.Should().Be(CostChangeDirection.Increase);
        result.Value.BaselineCostPerDay.Should().Be(100m);
        result.Value.ActualCostPerDay.Should().Be(130m);
    }

    [Fact]
    public async Task Get_NonExistentRelease_ShouldReturnNotFoundError()
    {
        var releaseId = Guid.NewGuid();
        _repository.GetByReleaseIdAsync(releaseId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ChangeCostImpact?>(null));

        var handler = new GetChangeCostImpact.Handler(_repository);
        var result = await handler.Handle(new GetChangeCostImpact.Query(releaseId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Governance.ChangeCostImpact.ReleaseNotFound");
    }

    // ── ListCostliestChanges ──

    [Fact]
    public async Task List_ShouldReturnItemsMappedCorrectly()
    {
        var impacts = new List<ChangeCostImpact>
        {
            ChangeCostImpact.Record(
                Guid.NewGuid(), "svc-a", "prod", "deploy A", 100m, 200m,
                "AWS", null, WindowStart, WindowEnd, null, FixedNow),
            ChangeCostImpact.Record(
                Guid.NewGuid(), "svc-b", "prod", "deploy B", 50m, 30m,
                "Azure", null, WindowStart, WindowEnd, null, FixedNow)
        };

        _repository.ListCostliestAsync(10, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ChangeCostImpact>>(impacts));

        var handler = new ListCostliestChanges.Handler(_repository);
        var result = await handler.Handle(new ListCostliestChanges.Query(10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items[0].ServiceName.Should().Be("svc-a");
        result.Value.Items[0].CostDelta.Should().Be(100m);
        result.Value.Items[0].Direction.Should().Be(CostChangeDirection.Increase);
        result.Value.Items[1].ServiceName.Should().Be("svc-b");
        result.Value.Items[1].Direction.Should().Be(CostChangeDirection.Decrease);
    }

    [Fact]
    public async Task List_EmptyResult_ShouldReturnEmptyList()
    {
        _repository.ListCostliestAsync(5, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ChangeCostImpact>>([]));

        var handler = new ListCostliestChanges.Handler(_repository);
        var result = await handler.Handle(new ListCostliestChanges.Query(5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }
}
