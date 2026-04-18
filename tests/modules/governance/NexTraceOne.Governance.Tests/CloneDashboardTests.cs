using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.CloneDashboard;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Tests;

public sealed class CloneDashboardTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 7, 12, 0, 0, TimeSpan.Zero);

    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ICustomDashboardRepository _repository = Substitute.For<ICustomDashboardRepository>();
    private readonly IGovernanceUnitOfWork _unitOfWork = Substitute.For<IGovernanceUnitOfWork>();

    public CloneDashboardTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnClone()
    {
        var sourceId = Guid.NewGuid();
        var source = CustomDashboard.Create(
            "Original", "desc", "grid", "Engineer",
            [
                new DashboardWidget(Guid.NewGuid().ToString(), "widget-1", new WidgetPosition(0, 0, 2, 2), new WidgetConfig()),
                new DashboardWidget(Guid.NewGuid().ToString(), "widget-2", new WidgetPosition(2, 0, 2, 2), new WidgetConfig())
            ], "tenant1", "user1", FixedNow.AddDays(-10));

        _repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(source));

        var handler = new CloneDashboard.Handler(_repository, _unitOfWork, _clock);
        var command = new CloneDashboard.Command(sourceId, "My Clone", "tenant1", "user1");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("My Clone");
        result.Value.CloneId.Should().NotBe(Guid.Empty);
        result.Value.SourceDashboardId.Should().Be(sourceId);

        await _repository.Received(1).AddAsync(Arg.Any<CustomDashboard>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SourceNotFound_ShouldReturnNotFoundError()
    {
        _repository.GetByIdAsync(Arg.Any<CustomDashboardId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CustomDashboard?>(null));

        var handler = new CloneDashboard.Handler(_repository, _unitOfWork, _clock);
        var command = new CloneDashboard.Command(Guid.NewGuid(), "Clone", "tenant1", "user1");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomDashboard.NotFound");
    }
}
