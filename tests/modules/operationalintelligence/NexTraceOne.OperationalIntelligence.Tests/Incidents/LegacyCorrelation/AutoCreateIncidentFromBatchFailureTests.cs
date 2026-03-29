using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;
using NexTraceOne.Integrations.Domain.LegacyTelemetry.Events;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateIncident;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

using AutoCreate = NexTraceOne.OperationalIntelligence.Application.Incidents.Features.AutoCreateIncidentFromBatchFailure.AutoCreateIncidentFromBatchFailure;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.LegacyCorrelation;

/// <summary>
/// Testes unitários para AutoCreateIncidentFromBatchFailure.
/// Valida que incidentes são criados automaticamente para falhas batch (abend/failed)
/// e que eventos concluídos com sucesso são ignorados.
/// </summary>
public sealed class AutoCreateIncidentFromBatchFailureTests
{
    private readonly ISender _sender;
    private readonly ILegacyEventCorrelator _correlator;
    private readonly ILogger<AutoCreate.Handler> _logger;
    private readonly AutoCreate.Handler _handler;

    public AutoCreateIncidentFromBatchFailureTests()
    {
        _sender = Substitute.For<ISender>();
        _correlator = Substitute.For<ILegacyEventCorrelator>();
        _logger = Substitute.For<ILogger<AutoCreate.Handler>>();
        _handler = new AutoCreate.Handler(_sender, _correlator, _logger);

        _correlator.CorrelateByJobNameAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CorrelationResult(false, null, null, null, null, "JobName", null));

        _sender.Send(Arg.Any<CreateIncident.Command>(), Arg.Any<CancellationToken>())
            .Returns(Result<CreateIncident.Response>.Success(
                new CreateIncident.Response(
                    Guid.NewGuid(), "INC-001", DateTimeOffset.UtcNow,
                    IncidentStatus.Open, IncidentSeverity.Major,
                    CorrelationConfidence.NotAssessed, false, 0, null)));
    }

    [Fact]
    public async Task Handle_FailedJob_CreatesIncident()
    {
        var notification = CreateBatchEvent(status: "failed", returnCode: "0008");

        await _handler.Handle(notification, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<CreateIncident.Command>(cmd => cmd.IncidentType == IncidentType.BackgroundProcessingIssue),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AbendedJob_CreatesCriticalIncident()
    {
        var notification = CreateBatchEvent(status: "abended", returnCode: "S0C7");

        await _handler.Handle(notification, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<CreateIncident.Command>(cmd => cmd.Severity == IncidentSeverity.Critical),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CompletedJob_DoesNotCreateIncident()
    {
        var notification = CreateBatchEvent(status: "completed", returnCode: "0000");

        await _handler.Handle(notification, CancellationToken.None);

        await _sender.DidNotReceive().Send(
            Arg.Any<CreateIncident.Command>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AbendReturnCode_CreatesIncident()
    {
        var notification = CreateBatchEvent(status: "completed", returnCode: "ABEND S0C7");

        await _handler.Handle(notification, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<CreateIncident.Command>(cmd => cmd.Severity == IncidentSeverity.Critical),
            Arg.Any<CancellationToken>());
    }

    private static LegacyBatchEventIngestedEvent CreateBatchEvent(
        string status = "completed",
        string? returnCode = "0000",
        string jobName = "TESTJOB1")
    {
        return new LegacyBatchEventIngestedEvent(
            IngestionEventId: Guid.NewGuid().ToString(),
            JobName: jobName,
            JobId: "JOB00001",
            ProgramName: "TESTPGM",
            ReturnCode: returnCode,
            Status: status,
            SystemName: "SYS1",
            LparName: "LPAR1",
            Severity: "error",
            Message: "Test batch event",
            Timestamp: DateTimeOffset.UtcNow);
    }
}
