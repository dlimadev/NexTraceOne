using MediatR;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Abstractions;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Features.IngestBatchEvents;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Requests;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;
using NexTraceOne.Integrations.Domain.LegacyTelemetry.Events;

namespace NexTraceOne.Integrations.Tests.LegacyTelemetry.Features;

/// <summary>
/// Testes que validam a publicação de domain events pelo handler IngestBatchEvents.
/// Garante que falhas e ABENDs publicam LegacyBatchEventIngestedEvent
/// e que eventos com sucesso não publicam.
/// </summary>
public sealed class IngestBatchEventsPublishingTests
{
    private readonly ILegacyEventWriter _writer;
    private readonly IPublisher _publisher;
    private readonly IngestBatchEvents.Handler _handler;

    public IngestBatchEventsPublishingTests()
    {
        _writer = Substitute.For<ILegacyEventWriter>();
        _publisher = Substitute.For<IPublisher>();
        _handler = new IngestBatchEvents.Handler(_writer, _publisher);
    }

    [Fact]
    public async Task Handle_FailedBatchEvent_PublishesDomainEvent()
    {
        var command = new IngestBatchEvents.Command(new List<BatchEventRequest>
        {
            new(null, null, "FAILJOB", null, null, null, null, "failed", "SYS1", "LPAR1",
                null, null, null, null, null)
        });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _publisher.Received(1).Publish(
            Arg.Is<LegacyBatchEventIngestedEvent>(e => e.JobName == "FAILJOB" && e.Status == "failed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SuccessfulBatchEvent_DoesNotPublishDomainEvent()
    {
        var command = new IngestBatchEvents.Command(new List<BatchEventRequest>
        {
            new(null, null, "OKJOB", null, null, null, "0000", "completed", "SYS1", "LPAR1",
                null, null, null, null, null)
        });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _publisher.DidNotReceive().Publish(
            Arg.Any<LegacyBatchEventIngestedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AbendBatchEvent_PublishesDomainEvent()
    {
        var command = new IngestBatchEvents.Command(new List<BatchEventRequest>
        {
            new(null, null, "ABENDJOB", null, null, null, null, "abended", "SYS1", "LPAR1",
                null, null, null, null, null)
        });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _publisher.Received(1).Publish(
            Arg.Is<LegacyBatchEventIngestedEvent>(e => e.JobName == "ABENDJOB"),
            Arg.Any<CancellationToken>());
    }
}
