using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Abstractions;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Parsers;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Requests;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;
using NexTraceOne.Integrations.Domain.LegacyTelemetry.Events;

namespace NexTraceOne.Integrations.Application.LegacyTelemetry.Features.IngestMqEvents;

/// <summary>
/// Feature: Ingestão de eventos operacionais IBM MQ.
/// </summary>
public static class IngestMqEvents
{
    public sealed record Command(List<MqEventRequest> Events) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Events).NotEmpty()
                .Must(e => e.Count <= 1000)
                .WithMessage("Maximum 1000 events per request.");
            RuleForEach(x => x.Events).ChildRules(ev =>
            {
                ev.RuleFor(e => e.QueueManagerName).MaximumLength(200);
                ev.RuleFor(e => e.QueueName).MaximumLength(200);
                ev.RuleFor(e => e.ChannelName).MaximumLength(200);
                ev.RuleFor(e => e.EventType).MaximumLength(100);
            });
        }
    }

    public sealed class Handler(
        ILegacyEventWriter writer,
        IPublisher publisher) : ICommandHandler<Command, Response>
    {
        private readonly MqEventParser _parser = new();

        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var normalized = new List<NormalizedLegacyEvent>(request.Events.Count);
            var errors = new List<string>();

            foreach (var evt in request.Events)
            {
                try
                {
                    normalized.Add(_parser.Parse(evt));
                }
                catch (Exception ex)
                {
                    errors.Add($"{evt.QueueManagerName}/{evt.QueueName}: {ex.Message}");
                }
            }

            if (normalized.Count > 0)
                await writer.WriteLegacyEventsAsync(normalized, cancellationToken);

            foreach (var (evt, norm) in request.Events.Zip(normalized))
            {
                var isDlq = string.Equals(evt.EventType, "dlq_message", StringComparison.OrdinalIgnoreCase);
                var isDepthCritical = evt.QueueDepth.HasValue && evt.MaxDepth.HasValue &&
                                     evt.MaxDepth.Value > 0 &&
                                     (double)evt.QueueDepth.Value / evt.MaxDepth.Value >= 0.9;
                var isCriticalSeverity = string.Equals(norm.Severity, LegacySeverity.Error, StringComparison.Ordinal) ||
                                         string.Equals(norm.Severity, LegacySeverity.Critical, StringComparison.Ordinal);

                if (!isDlq && !isDepthCritical && !isCriticalSeverity) continue;

                await publisher.Publish(new LegacyMqEventIngestedEvent(
                    IngestionEventId: norm.EventId,
                    QueueManagerName: evt.QueueManagerName,
                    QueueName: evt.QueueName,
                    ChannelName: evt.ChannelName,
                    EventType: evt.EventType,
                    QueueDepth: evt.QueueDepth,
                    MaxDepth: evt.MaxDepth,
                    ChannelStatus: evt.ChannelStatus,
                    Severity: norm.Severity,
                    Message: norm.Message,
                    Timestamp: norm.Timestamp), cancellationToken);
            }

            return new Response(normalized.Count, errors.Count, errors);
        }
    }

    public sealed record Response(int Ingested, int Errors, List<string> ErrorDetails);
}
