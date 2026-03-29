using Microsoft.Extensions.Logging;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;
using NexTraceOne.Integrations.Domain.LegacyTelemetry.Events;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

using CorrelateBatchEventFeature = NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CorrelateBatchEvent.CorrelateBatchEvent;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.LegacyCorrelation;

/// <summary>
/// Testes unitários para CorrelateBatchEvent handler.
/// Valida que eventos batch são correlacionados quando o jobName é fornecido
/// e que são ignorados quando o jobName está ausente.
/// </summary>
public sealed class CorrelateBatchEventTests
{
    private readonly ILegacyEventCorrelator _correlator;
    private readonly ILogger<CorrelateBatchEventFeature.Handler> _logger;
    private readonly CorrelateBatchEventFeature.Handler _handler;

    public CorrelateBatchEventTests()
    {
        _correlator = Substitute.For<ILegacyEventCorrelator>();
        _logger = Substitute.For<ILogger<CorrelateBatchEventFeature.Handler>>();
        _handler = new CorrelateBatchEventFeature.Handler(_correlator, _logger);

        _correlator.CorrelateByJobNameAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CorrelationResult(true, "BatchJob", "TESTJOB", Guid.NewGuid(), "Test Job", "JobName", "Matched"));
    }

    [Fact]
    public async Task Handle_WithJobName_CallsCorrelator()
    {
        var notification = CreateBatchEvent(jobName: "BATCHJOB1");

        await _handler.Handle(notification, CancellationToken.None);

        await _correlator.Received(1).CorrelateByJobNameAsync("BATCHJOB1", "SYS1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutJobName_SkipsCorrelation()
    {
        var notification = CreateBatchEvent(jobName: "");

        await _handler.Handle(notification, CancellationToken.None);

        await _correlator.DidNotReceive()
            .CorrelateByJobNameAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    private static LegacyBatchEventIngestedEvent CreateBatchEvent(string? jobName = "TESTJOB1")
    {
        return new LegacyBatchEventIngestedEvent(
            IngestionEventId: Guid.NewGuid().ToString(),
            JobName: jobName,
            JobId: "JOB00001",
            ProgramName: "TESTPGM",
            ReturnCode: "0000",
            Status: "completed",
            SystemName: "SYS1",
            LparName: "LPAR1",
            Severity: "info",
            Message: "Test batch event",
            Timestamp: DateTimeOffset.UtcNow);
    }
}
