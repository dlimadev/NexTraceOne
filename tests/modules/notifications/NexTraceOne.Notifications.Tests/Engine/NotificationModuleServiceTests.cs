using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.Engine;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;

namespace NexTraceOne.Notifications.Tests.Engine;

public sealed class NotificationModuleServiceTests
{
    private readonly INotificationOrchestrator _orchestrator = Substitute.For<INotificationOrchestrator>();
    private readonly INotificationStore _store = Substitute.For<INotificationStore>();
    private readonly NotificationModuleService _service;

    public NotificationModuleServiceTests()
    {
        _service = new NotificationModuleService(_orchestrator, _store);
    }

    [Fact]
    public async Task SubmitAsync_DelegatesToOrchestrator()
    {
        var request = new NotificationRequest
        {
            EventType = "IncidentCreated",
            Category = "Incident",
            Severity = "Critical",
            Title = "Test",
            Message = "Test message",
            SourceModule = "Test"
        };

        var expectedResult = new NotificationResult(true) { NotificationIds = [Guid.NewGuid()] };
        _orchestrator.ProcessAsync(request, Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        var result = await _service.SubmitAsync(request);

        result.Should().Be(expectedResult);
        await _orchestrator.Received(1).ProcessAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUnreadCountAsync_DelegatesToStore()
    {
        var userId = Guid.NewGuid();
        _store.CountUnreadAsync(userId, Arg.Any<CancellationToken>()).Returns(5);

        var count = await _service.GetUnreadCountAsync(userId);

        count.Should().Be(5);
        await _store.Received(1).CountUnreadAsync(userId, Arg.Any<CancellationToken>());
    }
}
