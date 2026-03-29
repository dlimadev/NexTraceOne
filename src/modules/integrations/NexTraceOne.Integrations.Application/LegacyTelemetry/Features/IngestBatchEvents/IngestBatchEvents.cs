using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Abstractions;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Parsers;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Requests;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.Integrations.Application.LegacyTelemetry.Features.IngestBatchEvents;

/// <summary>
/// Feature: Ingestão de eventos de execução batch mainframe.
/// Recebe payload, normaliza via BatchEventParser, e persiste via ILegacyEventWriter.
/// </summary>
public static class IngestBatchEvents
{
    public sealed record Command(List<BatchEventRequest> Events) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Events).NotEmpty()
                .Must(e => e.Count <= 1000)
                .WithMessage("Maximum 1000 events per request.");
            RuleForEach(x => x.Events).ChildRules(ev =>
            {
                ev.RuleFor(e => e.JobName).NotEmpty().MaximumLength(200);
                ev.RuleFor(e => e.Status).MaximumLength(50);
                ev.RuleFor(e => e.ReturnCode).MaximumLength(50);
                ev.RuleFor(e => e.SystemName).MaximumLength(100);
                ev.RuleFor(e => e.LparName).MaximumLength(100);
            });
        }
    }

    public sealed class Handler(
        ILegacyEventWriter writer) : ICommandHandler<Command, Response>
    {
        private readonly BatchEventParser _parser = new();

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
                    errors.Add($"{evt.JobName}: {ex.Message}");
                }
            }

            if (normalized.Count > 0)
                await writer.WriteLegacyEventsAsync(normalized, cancellationToken);

            return new Response(normalized.Count, errors.Count, errors);
        }
    }

    public sealed record Response(int Ingested, int Errors, List<string> ErrorDetails);
}
