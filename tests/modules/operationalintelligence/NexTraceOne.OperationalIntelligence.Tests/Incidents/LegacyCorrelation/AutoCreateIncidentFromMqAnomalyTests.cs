using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;
using NexTraceOne.Integrations.Domain.LegacyTelemetry.Events;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateIncident;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

using AutoCreate = NexTraceOne.OperationalIntelligence.Application.Incidents.Features.AutoCreateIncidentFromMqAnomaly.AutoCreateIncidentFromMqAnomaly;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.LegacyCorrelation;

/// <summary>
/// Testes unitários para AutoCreateIncidentFromMqAnomaly.
/// Valida criação automática de incidentes para anomalias MQ:
/// DLQ, profundidade de fila ≥ 90%, e severidade crítica.
/// </summary>
public sealed class AutoCreateIncidentFromMqAnomalyTests
{
    private readonly ISender _sender;
    private readonly ILegacyEventCorrelator _correlator;
    private readonly ILogger<AutoCreate.Handler> _logger;
    private readonly AutoCreate.Handler _handler;

    public AutoCreateIncidentFromMqAnomalyTests()
    {
        _sender = Substitute.For<ISender>();
        _correlator = Substitute.For<ILegacyEventCorrelator>();
        _logger = Substitute.For<ILogger<AutoCreate.Handler>>();
        _handler = new AutoCreate.Handler(_sender, _correlator, _logger);

        _correlator.CorrelateByQueueAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CorrelationResult(false, null, null, null, null, "QueueName", null));

        _sender.Send(Arg.Any<CreateIncident.Command>(), Arg.Any<CancellationToken>())
            .Returns(Result<CreateIncident.Response>.Success(
                new CreateIncident.Response(
                    Guid.NewGuid(), "INC-002", DateTimeOffset.UtcNow,
                    IncidentStatus.Open, IncidentSeverity.Major,
                    CorrelationConfidence.NotAssessed, false, 0, null)));
    }

    [Fact]
    public async Task Handle_DlqMessage_CreatesIncident()
    {
        var notification = CreateMqEvent(eventType: "dlq_message");

        await _handler.Handle(notification, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<CreateIncident.Command>(cmd => cmd.IncidentType == IncidentType.MessagingIssue),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DepthThreshold90Percent_CreatesIncident()
    {
        var notification = CreateMqEvent(eventType: "statistics", queueDepth: 900, maxDepth: 1000);

        await _handler.Handle(notification, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<CreateIncident.Command>(cmd => cmd.Severity == IncidentSeverity.Critical),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NormalStatistics_DoesNotCreateIncident()
    {
        var notification = CreateMqEvent(eventType: "statistics", queueDepth: 10, maxDepth: 1000, severity: "info");

        await _handler.Handle(notification, CancellationToken.None);

        await _sender.DidNotReceive().Send(
            Arg.Any<CreateIncident.Command>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CriticalSeverity_CreatesIncident()
    {
        var notification = CreateMqEvent(eventType: "channel_error", severity: "critical");

        await _handler.Handle(notification, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<CreateIncident.Command>(cmd => cmd.IncidentType == IncidentType.MessagingIssue),
            Arg.Any<CancellationToken>());
    }

    private static LegacyMqEventIngestedEvent CreateMqEvent(
        string eventType = "statistics",
        int? queueDepth = null,
        int? maxDepth = null,
        string severity = "warning")
    {
        return new LegacyMqEventIngestedEvent(
            IngestionEventId: Guid.NewGuid().ToString(),
            QueueManagerName: "QM1",
            QueueName: "ORDERS.IN",
            ChannelName: "CHAN1",
            EventType: eventType,
            QueueDepth: queueDepth,
            MaxDepth: maxDepth,
            ChannelStatus: "running",
            Severity: severity,
            Message: "Test MQ event",
            Timestamp: DateTimeOffset.UtcNow);
    }
}
