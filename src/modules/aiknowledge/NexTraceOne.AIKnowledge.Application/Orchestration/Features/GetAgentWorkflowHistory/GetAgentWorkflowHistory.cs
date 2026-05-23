using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.GetAgentWorkflowHistory;

/// <summary>
/// Feature: GetAgentWorkflowHistory — recupera histórico de execuções de workflows multi-agent.
/// Suporta filtros por nome de workflow, status e caller team, com paginação.
/// </summary>
public static class GetAgentWorkflowHistory
{
    // ── QUERY ─────────────────────────────────────────────────────────────

    public sealed record Query(
        string? WorkflowName = null,
        string? Status = null,
        string? CallerTeamId = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.WorkflowName).MaximumLength(200).When(x => x.WorkflowName is not null);
            RuleFor(x => x.Status).MaximumLength(50).When(x => x.Status is not null);
        }
    }

    // ── HANDLER ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IAgentWorkflowExecutionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            IReadOnlyList<Domain.Orchestration.Entities.AgentWorkflowExecution> items;

            if (!string.IsNullOrWhiteSpace(request.CallerTeamId))
            {
                items = await repository.ListByCallerTeamAsync(
                    request.CallerTeamId, request.Page, request.PageSize, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(request.WorkflowName))
            {
                items = await repository.ListByWorkflowAsync(
                    request.WorkflowName, request.Page, request.PageSize, cancellationToken);
            }
            else
            {
                items = await repository.ListRecentAsync(
                    request.Page, request.PageSize, cancellationToken);
            }

            var executionItems = items
                .Where(e => string.IsNullOrWhiteSpace(request.Status) || e.Status.ToString() == request.Status)
                .Select(e => new WorkflowExecutionItem(
                    e.Id.Value,
                    e.WorkflowName,
                    e.Status.ToString(),
                    e.InitialInput,
                    e.FinalOutput,
                    e.TotalSteps,
                    e.SuccessfulSteps,
                    e.TotalRetries,
                    e.DurationMs,
                    e.StartedAt,
                    e.CompletedAt,
                    e.CorrelationId,
                    e.CallerTeamId,
                    e.ErrorMessage))
                .ToList();

            return new Response(executionItems, executionItems.Count, request.Page, request.PageSize);
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    public sealed record Response(
        IReadOnlyList<WorkflowExecutionItem> Items,
        int TotalCount,
        int Page,
        int PageSize);

    public sealed record WorkflowExecutionItem(
        Guid ExecutionId,
        string WorkflowName,
        string Status,
        string InitialInput,
        string FinalOutput,
        int TotalSteps,
        int SuccessfulSteps,
        int TotalRetries,
        long DurationMs,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt,
        string CorrelationId,
        string? CallerTeamId,
        string? ErrorMessage);
}
