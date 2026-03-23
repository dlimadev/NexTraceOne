using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Models;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents;

/// <summary>
/// Testes unitários do IncidentAlertHandler.
/// Garante que alertas com severidade Error e Critical geram incidentes,
/// que alertas Info e Warning são ignorados, e que falhas no store
/// são tratadas sem propagar exceções.
/// </summary>
public sealed class IncidentAlertHandlerTests
{
    private readonly IIncidentStore _store;
    private readonly ILogger<IncidentAlertHandler> _logger;
    private readonly IncidentAlertHandler _handler;

    public IncidentAlertHandlerTests()
    {
        _store = Substitute.For<IIncidentStore>();
        _logger = Substitute.For<ILogger<IncidentAlertHandler>>();

        _store.CreateIncident(Arg.Any<CreateIncidentInput>())
            .Returns(new CreateIncidentResult(Guid.NewGuid(), "INC-001", DateTimeOffset.UtcNow));

        _handler = new IncidentAlertHandler(_store, _logger);
    }

    [Fact]
    public async Task HandleAlertAsync_CreatesIncident_WhenSeverityIsError()
    {
        var payload = CreateAlertPayload(AlertSeverity.Error);
        var dispatch = CreateDispatchResult();

        await _handler.HandleAlertAsync(payload, dispatch, CancellationToken.None);

        _store.Received(1).CreateIncident(Arg.Any<CreateIncidentInput>());
    }

    [Fact]
    public async Task HandleAlertAsync_CreatesIncident_WhenSeverityIsCritical()
    {
        var payload = CreateAlertPayload(AlertSeverity.Critical);
        var dispatch = CreateDispatchResult();

        await _handler.HandleAlertAsync(payload, dispatch, CancellationToken.None);

        _store.Received(1).CreateIncident(Arg.Any<CreateIncidentInput>());
    }

    [Fact]
    public async Task HandleAlertAsync_DoesNotCreateIncident_WhenSeverityIsInfo()
    {
        var payload = CreateAlertPayload(AlertSeverity.Info);
        var dispatch = CreateDispatchResult();

        await _handler.HandleAlertAsync(payload, dispatch, CancellationToken.None);

        _store.DidNotReceive().CreateIncident(Arg.Any<CreateIncidentInput>());
    }

    [Fact]
    public async Task HandleAlertAsync_DoesNotCreateIncident_WhenSeverityIsWarning()
    {
        var payload = CreateAlertPayload(AlertSeverity.Warning);
        var dispatch = CreateDispatchResult();

        await _handler.HandleAlertAsync(payload, dispatch, CancellationToken.None);

        _store.DidNotReceive().CreateIncident(Arg.Any<CreateIncidentInput>());
    }

    [Fact]
    public async Task HandleAlertAsync_CreatedIncidentTitle_ContainsAlertTitle()
    {
        var payload = CreateAlertPayload(AlertSeverity.Error, title: "Database connection pool exhausted");
        var dispatch = CreateDispatchResult();

        await _handler.HandleAlertAsync(payload, dispatch, CancellationToken.None);

        _store.Received(1).CreateIncident(Arg.Is<CreateIncidentInput>(input =>
            input.Title.Contains("Database connection pool exhausted")));
    }

    [Fact]
    public async Task HandleAlertAsync_MapsSeverity_ErrorToMajor()
    {
        var payload = CreateAlertPayload(AlertSeverity.Error);
        var dispatch = CreateDispatchResult();

        await _handler.HandleAlertAsync(payload, dispatch, CancellationToken.None);

        _store.Received(1).CreateIncident(Arg.Is<CreateIncidentInput>(input =>
            input.Severity == IncidentSeverity.Major));
    }

    [Fact]
    public async Task HandleAlertAsync_MapsSeverity_CriticalToCritical()
    {
        var payload = CreateAlertPayload(AlertSeverity.Critical);
        var dispatch = CreateDispatchResult();

        await _handler.HandleAlertAsync(payload, dispatch, CancellationToken.None);

        _store.Received(1).CreateIncident(Arg.Is<CreateIncidentInput>(input =>
            input.Severity == IncidentSeverity.Critical));
    }

    [Fact]
    public async Task HandleAlertAsync_DoesNotThrow_WhenIncidentStoreFails()
    {
        _store.CreateIncident(Arg.Any<CreateIncidentInput>())
            .Returns<CreateIncidentResult>(_ => throw new InvalidOperationException("Store unavailable"));

        var payload = CreateAlertPayload(AlertSeverity.Error);
        var dispatch = CreateDispatchResult();

        var act = () => _handler.HandleAlertAsync(payload, dispatch, CancellationToken.None);

        await act.Should().NotThrowAsync(
            "IncidentAlertHandler must catch exceptions from the store and log them without propagating");
    }

    [Fact]
    public async Task HandleAlertAsync_IncludesContextValues_InIncidentDescription()
    {
        var payload = CreateAlertPayload(AlertSeverity.Error, context: new Dictionary<string, string>
        {
            ["ServiceId"] = "svc-payments",
            ["ServiceName"] = "Payment Service",
            ["Environment"] = "production"
        });
        var dispatch = CreateDispatchResult();

        await _handler.HandleAlertAsync(payload, dispatch, CancellationToken.None);

        _store.Received(1).CreateIncident(Arg.Is<CreateIncidentInput>(input =>
            input.Description.Contains("svc-payments") ||
            input.ServiceId == "svc-payments"));
    }

    [Fact]
    public async Task HandleAlertAsync_UsesContextServiceId_AsIncidentServiceId()
    {
        var payload = CreateAlertPayload(AlertSeverity.Critical, context: new Dictionary<string, string>
        {
            ["ServiceId"] = "svc-orders",
            ["ServiceName"] = "Order Service",
            ["OwnerTeam"] = "team-commerce"
        });
        var dispatch = CreateDispatchResult();

        await _handler.HandleAlertAsync(payload, dispatch, CancellationToken.None);

        _store.Received(1).CreateIncident(Arg.Is<CreateIncidentInput>(input =>
            input.ServiceId == "svc-orders" &&
            input.ServiceDisplayName == "Order Service" &&
            input.OwnerTeam == "team-commerce"));
    }

    [Fact]
    public async Task HandleAlertAsync_PrefixesIncidentTitle_WithAlertTag()
    {
        var payload = CreateAlertPayload(AlertSeverity.Error, title: "High latency detected");
        var dispatch = CreateDispatchResult();

        await _handler.HandleAlertAsync(payload, dispatch, CancellationToken.None);

        _store.Received(1).CreateIncident(Arg.Is<CreateIncidentInput>(input =>
            input.Title.StartsWith("[Alert]")));
    }

    private static AlertPayload CreateAlertPayload(
        AlertSeverity severity,
        string title = "Test Alert",
        string description = "Test alert description",
        string source = "health",
        Dictionary<string, string>? context = null)
    {
        return new AlertPayload
        {
            Title = title,
            Description = description,
            Severity = severity,
            Source = source,
            CorrelationId = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            Context = context ?? new Dictionary<string, string>()
        };
    }

    private static AlertDispatchResult CreateDispatchResult(bool allSucceeded = true)
    {
        return new AlertDispatchResult
        {
            ChannelResults = new Dictionary<string, bool>
            {
                ["slack"] = allSucceeded,
                ["email"] = allSucceeded
            }
        };
    }
}
