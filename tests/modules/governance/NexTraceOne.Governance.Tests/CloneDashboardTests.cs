using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.CloneDashboard;

namespace NexTraceOne.Governance.Tests;

public sealed class CloneDashboardTests
{
    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new DateTimeOffset(2026, 4, 7, 12, 0, 0, TimeSpan.Zero);
        public DateOnly UtcToday => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnClone()
    {
        var handler = new CloneDashboard.Handler(new FixedClock());
        var command = new CloneDashboard.Command(Guid.NewGuid(), "My Clone", "tenant1", "user1");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("My Clone", result.Value.Name);
        Assert.NotEqual(Guid.Empty, result.Value.CloneId);
    }

    [Fact]
    public async Task Handle_ShouldPreserveSourceDashboardId()
    {
        var handler = new CloneDashboard.Handler(new FixedClock());
        var sourceId = Guid.NewGuid();
        var command = new CloneDashboard.Command(sourceId, "Clone", "tenant1", "user1");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(sourceId, result.Value.SourceDashboardId);
    }
}
