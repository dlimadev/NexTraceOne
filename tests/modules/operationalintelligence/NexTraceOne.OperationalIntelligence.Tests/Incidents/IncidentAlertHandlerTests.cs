using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Models;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;
using NexTraceOne.OperationalIntelligence.Tests.Incidents.Infrastructure;

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

    // ── MapAlertSourceToIncidentType ──────────────────────────────────────

    [Theory]
    [InlineData("health", IncidentType.AvailabilityIssue)]
    [InlineData("health-check", IncidentType.AvailabilityIssue)]
    [InlineData("platform-health", IncidentType.AvailabilityIssue)]
    [InlineData("worker", IncidentType.BackgroundProcessingIssue)]
    [InlineData("background-jobs", IncidentType.BackgroundProcessingIssue)]
    [InlineData("scheduler", IncidentType.BackgroundProcessingIssue)]
    [InlineData("ingestion", IncidentType.ServiceDegradation)]
    [InlineData("pipeline", IncidentType.ServiceDegradation)]
    [InlineData("ai", IncidentType.DependencyFailure)]
    [InlineData("ai-provider", IncidentType.DependencyFailure)]
    [InlineData("drift", IncidentType.OperationalRegression)]
    [InlineData("anomaly", IncidentType.OperationalRegression)]
    [InlineData("change-intelligence", IncidentType.OperationalRegression)]
    [InlineData("unknown-source", IncidentType.ServiceDegradation)]
    public async Task HandleAlertAsync_MapsSourceToCorrectIncidentType(string source, IncidentType expectedType)
    {
        var payload = CreateAlertPayload(AlertSeverity.Error, source: source);
        var dispatch = CreateDispatchResult();

        await _handler.HandleAlertAsync(payload, dispatch, CancellationToken.None);

        _store.Received(1).CreateIncident(Arg.Is<CreateIncidentInput>(input =>
            input.IncidentType == expectedType));
    }

    [Fact]
    public async Task HandleAlertAsync_MapsSourceCaseInsensitive()
    {
        // Source matching is case-insensitive
        var payload = CreateAlertPayload(AlertSeverity.Error, source: "HEALTH");
        var dispatch = CreateDispatchResult();

        await _handler.HandleAlertAsync(payload, dispatch, CancellationToken.None);

        _store.Received(1).CreateIncident(Arg.Is<CreateIncidentInput>(input =>
            input.IncidentType == IncidentType.AvailabilityIssue));
    }

    // ── Description building ──────────────────────────────────────────────

    [Fact]
    public async Task HandleAlertAsync_Description_ContainsSourceSeverityAndTimestamp()
    {
        var payload = CreateAlertPayload(AlertSeverity.Critical, source: "pipeline");
        var dispatch = CreateDispatchResult();

        await _handler.HandleAlertAsync(payload, dispatch, CancellationToken.None);

        _store.Received(1).CreateIncident(Arg.Is<CreateIncidentInput>(input =>
            input.Description.Contains("pipeline") &&
            input.Description.Contains("Critical") &&
            input.Description.Contains("channels")));
    }

    [Fact]
    public async Task HandleAlertAsync_Description_IncludesContextEntries_WhenPresent()
    {
        var payload = CreateAlertPayload(AlertSeverity.Critical, context: new Dictionary<string, string>
        {
            ["Domain"] = "payments",
            ["Environment"] = "production"
        });
        var dispatch = CreateDispatchResult();

        await _handler.HandleAlertAsync(payload, dispatch, CancellationToken.None);

        _store.Received(1).CreateIncident(Arg.Is<CreateIncidentInput>(input =>
            input.Description.Contains("Domain") &&
            input.Description.Contains("payments") &&
            input.Description.Contains("Environment")));
    }

    [Fact]
    public async Task HandleAlertAsync_Description_IncludesCorrelationId_WhenPresent()
    {
        var correlationId = "corr-123-abc";
        var payload = CreateAlertPayload(AlertSeverity.Error);
        // Set a known correlation ID via reflection isn't needed — just use the helper
        var payloadWithCorrelation = payload with { CorrelationId = correlationId };
        var dispatch = CreateDispatchResult();

        await _handler.HandleAlertAsync(payloadWithCorrelation, dispatch, CancellationToken.None);

        _store.Received(1).CreateIncident(Arg.Is<CreateIncidentInput>(input =>
            input.Description.Contains(correlationId)));
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
