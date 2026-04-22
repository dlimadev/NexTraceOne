using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListAgentExecutionHistory;

/// <summary>
/// Feature: ListAgentExecutionHistory — lista o histórico de planos de execução agentic de um tenant.
/// Suporta filtro por status e paginação básica.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ListAgentExecutionHistory
{
    /// <summary>Query de listagem de planos com filtro opcional por status.</summary>
    public sealed record Query(
        Guid TenantId,
        string? StatusFilter,
        int PageSize = 50) : IQuery<Response>;

    /// <summary>Validador da query de histórico.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        private static readonly HashSet<string> ValidStatuses =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Pending", "Running", "WaitingApproval", "Completed", "Failed", "Cancelled",
            };

        public Validator()
        {
            RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
            RuleFor(x => x.StatusFilter)
                .Must(s => s is null || ValidStatuses.Contains(s))
                .WithMessage("StatusFilter must be one of: Pending, Running, WaitingApproval, Completed, Failed, Cancelled.");
        }
    }

    /// <summary>Handler que lista os planos agentic de um tenant.</summary>
    public sealed class Handler(
        IAgentExecutionPlanRepository planRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            Guard.Against.Null(request);

            PlanStatus? statusFilter = null;
            if (request.StatusFilter is not null &&
                Enum.TryParse<PlanStatus>(request.StatusFilter, true, out var parsed))
                statusFilter = parsed;

            var plans = await planRepository.ListByTenantAsync(
                request.TenantId, statusFilter, request.PageSize, ct);

            var items = plans.Select(p => new PlanSummary(
                PlanId: p.Id.Value,
                Description: p.Description,
                RequestedBy: p.RequestedBy,
                Status: p.PlanStatus.ToString(),
                StepCount: p.Steps.Count,
                ConsumedTokens: p.ConsumedTokens,
                MaxTokenBudget: p.MaxTokenBudget,
                CorrelationId: p.CorrelationId,
                StartedAt: p.StartedAt,
                CompletedAt: p.CompletedAt)).ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resumo de um plano de execução agentic.</summary>
    public sealed record PlanSummary(
        Guid PlanId,
        string Description,
        string RequestedBy,
        string Status,
        int StepCount,
        int ConsumedTokens,
        int MaxTokenBudget,
        string CorrelationId,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt);

    /// <summary>Resposta da listagem de histórico de planos.</summary>
    public sealed record Response(IReadOnlyList<PlanSummary> Items, int TotalCount);
}
