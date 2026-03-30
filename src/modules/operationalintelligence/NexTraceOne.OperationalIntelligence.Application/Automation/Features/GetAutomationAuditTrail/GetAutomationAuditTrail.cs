using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Automation.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationAuditTrail;

/// <summary>
/// Feature: GetAutomationAuditTrail — retorna a trilha de auditoria de automação operacional
/// filtrável por workflow, serviço ou equipa para rastreabilidade completa.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetAutomationAuditTrail
{
    /// <summary>Query para obter a trilha de auditoria de automação. Pelo menos um filtro deve ser informado.</summary>
    public sealed record Query(
        string? WorkflowId,
        string? ServiceId,
        string? TeamId) : IQuery<Response>;

    /// <summary>Valida que pelo menos um filtro foi informado.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowId).MaximumLength(200).When(x => x.WorkflowId is not null);
            RuleFor(x => x.ServiceId).MaximumLength(200).When(x => x.ServiceId is not null);
            RuleFor(x => x.TeamId).MaximumLength(200).When(x => x.TeamId is not null);

            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.WorkflowId) ||
                           !string.IsNullOrWhiteSpace(x.ServiceId) ||
                           !string.IsNullOrWhiteSpace(x.TeamId))
                .WithMessage("At least one filter (WorkflowId, ServiceId or TeamId) must be provided.");
        }
    }

    /// <summary>Handler que lê a trilha de auditoria de automação a partir do IAutomationAuditRepository.</summary>
    public sealed class Handler(IAutomationAuditRepository auditRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            IReadOnlyList<AutomationAuditRecord> records;

            // WorkflowId is the most specific filter; ServiceId and TeamId are broader.
            if (!string.IsNullOrWhiteSpace(request.WorkflowId) && Guid.TryParse(request.WorkflowId, out var wfId))
            {
                records = await auditRepository.GetByWorkflowIdAsync(
                    new AutomationWorkflowRecordId(wfId), cancellationToken);

                if (!string.IsNullOrWhiteSpace(request.ServiceId))
                    records = records.Where(r => string.Equals(r.ServiceId, request.ServiceId, StringComparison.OrdinalIgnoreCase)).ToList();

                if (!string.IsNullOrWhiteSpace(request.TeamId))
                    records = records.Where(r => string.Equals(r.TeamId, request.TeamId, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else if (!string.IsNullOrWhiteSpace(request.ServiceId))
            {
                records = await auditRepository.GetByServiceIdAsync(request.ServiceId, cancellationToken);

                if (!string.IsNullOrWhiteSpace(request.TeamId))
                    records = records.Where(r => string.Equals(r.TeamId, request.TeamId, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else
            {
                records = await auditRepository.GetByTeamIdAsync(request.TeamId!, cancellationToken);
            }

            var entries = records
                .Select(r => new AuditEntry(
                    r.Id.Value,
                    r.WorkflowId.Value,
                    r.Action,
                    r.Actor,
                    r.OccurredAt,
                    r.Details,
                    r.ServiceId,
                    r.TeamId))
                .ToList();

            return Result<Response>.Success(new Response(entries));
        }
    }

    /// <summary>Entrada na trilha de auditoria de automação operacional.</summary>
    public sealed record AuditEntry(
        Guid EntryId,
        Guid? WorkflowId,
        AutomationAuditAction Action,
        string PerformedBy,
        DateTimeOffset PerformedAt,
        string? Details,
        string? ServiceId,
        string? TeamId);

    /// <summary>Resposta com a trilha de auditoria de automação.</summary>
    public sealed record Response(IReadOnlyList<AuditEntry> Entries);
}
