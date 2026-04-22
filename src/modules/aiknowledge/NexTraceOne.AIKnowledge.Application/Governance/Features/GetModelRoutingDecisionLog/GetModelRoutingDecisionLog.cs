using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetModelRoutingDecisionLog;

/// <summary>
/// Feature: GetModelRoutingDecisionLog — retorna o histórico paginado de decisões de roteamento.
/// Suporta filtro por intenção (baseado em UseCaseType) e paginação básica.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class GetModelRoutingDecisionLog
{
    private static readonly HashSet<string> ValidIntents =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "CodeGeneration", "DocumentSummarization", "IncidentAnalysis",
            "ContractDraft", "ComplianceCheck", "GeneralQuery",
        };

    /// <summary>Query de listagem de decisões de roteamento com filtro de intenção.</summary>
    public sealed record Query(
        string? IntentFilter,
        int PageSize = 50) : IQuery<Response>;

    /// <summary>Validador da query de log de roteamento.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
            RuleFor(x => x.IntentFilter)
                .Must(i => i is null || ValidIntents.Contains(i))
                .WithMessage($"IntentFilter must be one of: {string.Join(", ", ValidIntents)}.");
        }
    }

    /// <summary>Handler que lista as decisões de roteamento recentes.</summary>
    public sealed class Handler(
        IAiRoutingDecisionRepository routingDecisionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            Guard.Against.Null(request);

            var decisions = await routingDecisionRepository.ListRecentAsync(request.PageSize, ct);

            // Filtro por intenção: mapeia IntentFilter para AIUseCaseType via nome
            var filtered = request.IntentFilter is null
                ? decisions
                : decisions.Where(d =>
                    string.Equals(d.UseCaseType.ToString(), request.IntentFilter,
                        StringComparison.OrdinalIgnoreCase));

            var items = filtered.Select(d => new RoutingDecisionEntry(
                DecisionId: d.Id.Value,
                CorrelationId: d.CorrelationId,
                Persona: d.Persona,
                UseCaseType: d.UseCaseType.ToString(),
                SelectedModelName: d.SelectedModelName,
                SelectedProvider: d.SelectedProvider,
                IsInternalModel: d.IsInternalModel,
                SelectedPath: d.SelectedPath.ToString(),
                Rationale: d.Rationale,
                ConfidenceLevel: d.ConfidenceLevel.ToString(),
                EstimatedCostClass: d.EstimatedCostClass,
                DecidedAt: d.DecidedAt)).ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Entrada resumida de decisão de roteamento.</summary>
    public sealed record RoutingDecisionEntry(
        Guid DecisionId,
        string CorrelationId,
        string Persona,
        string UseCaseType,
        string SelectedModelName,
        string SelectedProvider,
        bool IsInternalModel,
        string SelectedPath,
        string Rationale,
        string ConfidenceLevel,
        string EstimatedCostClass,
        DateTimeOffset DecidedAt);

    /// <summary>Resposta do log de decisões de roteamento.</summary>
    public sealed record Response(IReadOnlyList<RoutingDecisionEntry> Items, int TotalCount);
}
