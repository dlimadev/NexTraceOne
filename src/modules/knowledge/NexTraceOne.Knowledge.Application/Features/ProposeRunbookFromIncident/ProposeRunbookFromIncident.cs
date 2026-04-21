using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Domain.Entities;

namespace NexTraceOne.Knowledge.Application.Features.ProposeRunbookFromIncident;

/// <summary>
/// Feature: ProposeRunbookFromIncident — gera e persiste um runbook proposto
/// baseado nos dados de um incidente resolvido.
/// Pilar: Operational Knowledge. Owner: Knowledge.
/// </summary>
public static class ProposeRunbookFromIncident
{
    public sealed record Command(
        Guid IncidentId,
        string IncidentTitle,
        string ResolutionSummary,
        string? ServiceName = null,
        string? TeamName = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty();
            RuleFor(x => x.IncidentTitle).NotEmpty().MaximumLength(300);
            RuleFor(x => x.ResolutionSummary).NotEmpty().MaximumLength(5000);
        }
    }

    public sealed class Handler(
        IProposedRunbookRepository repo,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Idempotent — check if already proposed for this incident
            var existing = await repo.GetByIncidentIdAsync(request.IncidentId, cancellationToken);
            if (existing is not null)
                return Result<Response>.Success(new Response(
                    RunbookId: existing.Id.Value.ToString(),
                    Title: existing.Title,
                    Status: existing.Status.ToString(),
                    AlreadyExisted: true));

            var title = $"Runbook: {request.IncidentTitle}";
            var content = BuildRunbookContent(request.IncidentTitle, request.ResolutionSummary, request.ServiceName);

            var runbook = ProposedRunbook.Create(
                title: title,
                contentMarkdown: content,
                sourceIncidentId: request.IncidentId,
                proposedAt: clock.UtcNow,
                serviceName: request.ServiceName,
                teamName: request.TeamName);

            await repo.AddAsync(runbook, cancellationToken);

            return Result<Response>.Success(new Response(
                RunbookId: runbook.Id.Value.ToString(),
                Title: runbook.Title,
                Status: runbook.Status.ToString(),
                AlreadyExisted: false));
        }

        private static string BuildRunbookContent(string title, string resolutionSummary, string? serviceName)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"# {title}");
            sb.AppendLine();
            if (serviceName is not null)
                sb.AppendLine($"**Service:** {serviceName}");
            sb.AppendLine();
            sb.AppendLine("## Problem");
            sb.AppendLine($"> Auto-generated from incident: {title}");
            sb.AppendLine();
            sb.AppendLine("## Resolution Steps");
            sb.AppendLine(resolutionSummary);
            sb.AppendLine();
            sb.AppendLine("## Notes");
            sb.AppendLine("*Review and refine this runbook before publishing.*");
            return sb.ToString();
        }
    }

    public sealed record Response(string RunbookId, string Title, string Status, bool AlreadyExisted);
}
