using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Abstractions;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Parsers;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Requests;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;
using NexTraceOne.Integrations.Domain.LegacyTelemetry.Events;

namespace NexTraceOne.Integrations.Application.LegacyTelemetry.Features.IngestMainframeEvents;

/// <summary>
/// Feature: Ingestão de eventos operacionais mainframe genéricos.
/// Suporta SMF, SYSLOG, CICS stats, IMS stats e eventos operacionais.
/// </summary>
public static class IngestMainframeEvents
{
    public sealed record Command(List<MainframeEventRequest> Events) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Events).NotEmpty()
                .Must(e => e.Count <= 1000)
                .WithMessage("Maximum 1000 events per request.");
            RuleForEach(x => x.Events).ChildRules(ev =>
            {
                ev.RuleFor(e => e.SystemName).MaximumLength(100);
                ev.RuleFor(e => e.LparName).MaximumLength(100);
                ev.RuleFor(e => e.EventType).MaximumLength(200);
                ev.RuleFor(e => e.Severity).MaximumLength(50);
                ev.RuleFor(e => e.Message).MaximumLength(10000);
            });
        }
    }

    public sealed class Handler(
        ILegacyEventWriter writer,
        IPublisher publisher) : ICommandHandler<Command, Response>
    {
        private readonly MainframeEventParser _parser = new();

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
                    errors.Add($"{evt.SystemName}/{evt.EventType}: {ex.Message}");
                }
            }

            if (normalized.Count > 0)
                await writer.WriteLegacyEventsAsync(normalized, cancellationToken);

            foreach (var (evt, norm) in request.Events.Zip(normalized))
            {
                var isCriticalSeverity = string.Equals(norm.Severity, LegacySeverity.Error, StringComparison.Ordinal) ||
                                         string.Equals(norm.Severity, LegacySeverity.Critical, StringComparison.Ordinal);

                if (!isCriticalSeverity) continue;

                await publisher.Publish(new LegacyMainframeEventIngestedEvent(
                    IngestionEventId: norm.EventId,
                    SourceType: evt.SourceType,
                    SystemName: evt.SystemName,
                    LparName: evt.LparName,
                    EventType: evt.EventType,
                    Severity: norm.Severity,
                    Message: norm.Message,
                    Timestamp: norm.Timestamp), cancellationToken);
            }

            return new Response(normalized.Count, errors.Count, errors);
        }
    }

    public sealed record Response(int Ingested, int Errors, List<string> ErrorDetails);
}
