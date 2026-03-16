using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.ListAutomationWorkflows;

/// <summary>
/// Feature: ListAutomationWorkflows — lista workflows de automação com filtros opcionais.
/// Retorna resumo dos workflows com status, risco, ação e serviço associado.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
///
/// Nota: nesta fase os dados são simulados até integração completa entre módulos.
/// </summary>
public static class ListAutomationWorkflows
{
    /// <summary>Query para listar workflows de automação com filtros e paginação.</summary>
    public sealed record Query(
        string? ServiceId,
        string? Status,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta de listagem.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.ServiceId).MaximumLength(200).When(x => x.ServiceId is not null);
            RuleFor(x => x.Status).MaximumLength(200).When(x => x.Status is not null);
        }
    }

    /// <summary>Handler que compõe a listagem de workflows de automação.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var items = GenerateSimulatedItems(request);

            var response = new Response(
                Items: items,
                TotalCount: items.Count);

            return Task.FromResult(Result<Response>.Success(response));
        }

        private static IReadOnlyList<WorkflowSummary> GenerateSimulatedItems(Query request)
        {
            var allItems = new List<WorkflowSummary>
            {
                new(Guid.Parse("b0a10001-0001-0000-0000-000000000001"),
                    "action-restart-controlled",
                    "Controlled Service Restart",
                    AutomationWorkflowStatus.Executing,
                    RiskLevel.Medium,
                    "ops-engineer@nextraceone.io",
                    "svc-payment-gateway",
                    DateTimeOffset.Parse("2024-06-15T10:15:00Z")),

                new(Guid.Parse("b0a10002-0001-0000-0000-000000000001"),
                    "action-observe-validate",
                    "Observe and Validate",
                    AutomationWorkflowStatus.AwaitingApproval,
                    RiskLevel.Low,
                    "platform-engineer@nextraceone.io",
                    "svc-catalog-sync",
                    DateTimeOffset.Parse("2024-06-16T08:00:00Z")),

                new(Guid.Parse("b0a10003-0001-0000-0000-000000000001"),
                    "action-verify-dependency",
                    "Verify Dependency State",
                    AutomationWorkflowStatus.Cancelled,
                    RiskLevel.Low,
                    "senior-engineer@nextraceone.io",
                    "svc-order-api",
                    DateTimeOffset.Parse("2024-06-16T09:00:00Z")),

                new(Guid.Parse("b0a10004-0001-0000-0000-000000000001"),
                    "action-rollback-readiness",
                    "Rollback Readiness Review",
                    AutomationWorkflowStatus.Completed,
                    RiskLevel.High,
                    "tech-lead@nextraceone.io",
                    "svc-inventory-consumer",
                    DateTimeOffset.Parse("2024-06-14T16:30:00Z")),

                new(Guid.Parse("b0a10005-0001-0000-0000-000000000001"),
                    "action-reprocess-controlled",
                    "Controlled Event Reprocessing",
                    AutomationWorkflowStatus.Draft,
                    RiskLevel.Medium,
                    "ops-engineer@nextraceone.io",
                    "svc-notification-worker",
                    DateTimeOffset.Parse("2024-06-17T07:00:00Z")),

                new(Guid.Parse("b0a10006-0001-0000-0000-000000000001"),
                    "action-execute-runbook-step",
                    "Execute Runbook Step",
                    AutomationWorkflowStatus.Completed,
                    RiskLevel.Low,
                    "platform-engineer@nextraceone.io",
                    "svc-auth-gateway",
                    DateTimeOffset.Parse("2024-06-13T11:00:00Z")),
            };

            var filtered = allItems.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.ServiceId))
                filtered = filtered.Where(w => w.ServiceId != null &&
                    w.ServiceId.Equals(request.ServiceId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                Enum.TryParse<AutomationWorkflowStatus>(request.Status, true, out var statusFilter))
                filtered = filtered.Where(w => w.Status == statusFilter);

            return filtered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();
        }
    }

    /// <summary>Resumo de um workflow de automação na listagem.</summary>
    public sealed record WorkflowSummary(
        Guid WorkflowId,
        string ActionId,
        string ActionDisplayName,
        AutomationWorkflowStatus Status,
        RiskLevel RiskLevel,
        string RequestedBy,
        string? ServiceId,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta paginada da listagem de workflows de automação.</summary>
    public sealed record Response(
        IReadOnlyList<WorkflowSummary> Items,
        int TotalCount);
}
