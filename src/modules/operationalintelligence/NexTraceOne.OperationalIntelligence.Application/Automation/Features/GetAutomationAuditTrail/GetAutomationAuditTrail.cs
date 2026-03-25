using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationAuditTrail;

/// <summary>
/// Feature: GetAutomationAuditTrail — retorna a trilha de auditoria de automação operacional
/// filtrável por workflow, serviço ou equipa para rastreabilidade completa.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
///
/// LIMITATION: dados são simulados com entradas hardcoded.
/// Substituir pela leitura real dos eventos de auditoria persistidos no AutomationDbContext
/// quando a integração entre módulos estiver completa.
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

    /// <summary>Handler que compõe a trilha de auditoria com dados simulados hardcoded.
    /// LIMITATION: substituir por leitura real do AutomationDbContext.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var entries = GenerateSimulatedEntries(request);
            return Task.FromResult(Result<Response>.Success(new Response(entries)));
        }

        private static IReadOnlyList<AuditEntry> GenerateSimulatedEntries(Query request)
        {
            var allEntries = new List<AuditEntry>
            {
                new(Guid.Parse("ee000001-0001-0000-0000-000000000001"),
                    Guid.Parse("b0a10001-0001-0000-0000-000000000001"),
                    AutomationAuditAction.WorkflowCreated,
                    "ops-engineer@nextraceone.io",
                    DateTimeOffset.Parse("2024-06-15T10:15:00Z"),
                    "Workflow created for controlled restart of payment-gateway.",
                    "svc-payment-gateway",
                    "payment-squad"),

                new(Guid.Parse("ee000001-0002-0000-0000-000000000001"),
                    Guid.Parse("b0a10001-0001-0000-0000-000000000001"),
                    AutomationAuditAction.PreconditionsEvaluated,
                    "system",
                    DateTimeOffset.Parse("2024-06-15T10:26:00Z"),
                    "All 3 preconditions evaluated — all passed.",
                    "svc-payment-gateway",
                    "payment-squad"),

                new(Guid.Parse("ee000001-0003-0000-0000-000000000001"),
                    Guid.Parse("b0a10001-0001-0000-0000-000000000001"),
                    AutomationAuditAction.ApprovalGranted,
                    "tech-lead@nextraceone.io",
                    DateTimeOffset.Parse("2024-06-15T10:30:00Z"),
                    "Approved — low blast radius, controlled restart is safe.",
                    "svc-payment-gateway",
                    "payment-squad"),

                new(Guid.Parse("ee000001-0004-0000-0000-000000000001"),
                    Guid.Parse("b0a10001-0001-0000-0000-000000000001"),
                    AutomationAuditAction.ExecutionStarted,
                    "ops-engineer@nextraceone.io",
                    DateTimeOffset.Parse("2024-06-15T10:35:00Z"),
                    "Execution started for controlled restart workflow.",
                    "svc-payment-gateway",
                    "payment-squad"),

                new(Guid.Parse("ee000001-0005-0000-0000-000000000001"),
                    Guid.Parse("b0a10001-0001-0000-0000-000000000001"),
                    AutomationAuditAction.StepCompleted,
                    "ops-engineer@nextraceone.io",
                    DateTimeOffset.Parse("2024-06-15T10:40:00Z"),
                    "Step 2 completed: restart executed successfully.",
                    "svc-payment-gateway",
                    "payment-squad"),

                new(Guid.Parse("ee000002-0001-0000-0000-000000000001"),
                    Guid.Parse("b0a10002-0001-0000-0000-000000000001"),
                    AutomationAuditAction.WorkflowCreated,
                    "platform-engineer@nextraceone.io",
                    DateTimeOffset.Parse("2024-06-16T08:00:00Z"),
                    "Workflow created for post-deployment observation of catalog-sync.",
                    "svc-catalog-sync",
                    "platform-squad"),

                new(Guid.Parse("ee000003-0001-0000-0000-000000000001"),
                    Guid.Parse("b0a10003-0001-0000-0000-000000000001"),
                    AutomationAuditAction.WorkflowCreated,
                    "senior-engineer@nextraceone.io",
                    DateTimeOffset.Parse("2024-06-16T09:00:00Z"),
                    "Workflow created for dependency state verification of order-api.",
                    "svc-order-api",
                    "order-squad"),

                new(Guid.Parse("ee000003-0002-0000-0000-000000000001"),
                    Guid.Parse("b0a10003-0001-0000-0000-000000000001"),
                    AutomationAuditAction.WorkflowCancelled,
                    "senior-engineer@nextraceone.io",
                    DateTimeOffset.Parse("2024-06-16T09:15:00Z"),
                    "Workflow cancelled — issue resolved before execution.",
                    "svc-order-api",
                    "order-squad"),
            };

            var filtered = allEntries.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.WorkflowId) && Guid.TryParse(request.WorkflowId, out var wfId))
                filtered = filtered.Where(e => e.WorkflowId == wfId);

            if (!string.IsNullOrWhiteSpace(request.ServiceId))
                filtered = filtered.Where(e =>
                    e.ServiceId != null && e.ServiceId.Equals(request.ServiceId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.TeamId))
                filtered = filtered.Where(e =>
                    e.TeamId != null && e.TeamId.Equals(request.TeamId, StringComparison.OrdinalIgnoreCase));

            return filtered.ToList();
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
