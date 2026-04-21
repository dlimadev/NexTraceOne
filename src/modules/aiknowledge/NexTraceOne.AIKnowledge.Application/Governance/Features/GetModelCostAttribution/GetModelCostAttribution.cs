using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetModelCostAttribution;

/// <summary>
/// Feature: GetModelCostAttribution — relatório de atribuição de custo de tokens por modelo,
/// equipa, serviço e use-case. Fonte de verdade para FinOps de IA.
/// Pilar: AI Governance + FinOps contextual.
/// </summary>
public static class GetModelCostAttribution
{
    public sealed record Query(
        string? ModelId = null,
        string? TeamId = null,
        string? ServiceName = null,
        int PeriodDays = 30) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PeriodDays).InclusiveBetween(1, 365);
        }
    }

    public sealed class Handler(IAiTokenUsageLedgerRepository ledgerRepo) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-request.PeriodDays);
            var entries = await ledgerRepo.ListByPeriodAsync(cutoff, cancellationToken);

            var filtered = entries
                .Where(e => request.ModelId is null || e.ModelId.Equals(request.ModelId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var byModel = filtered
                .GroupBy(e => new { e.ModelId, e.ModelName })
                .Select(g => new ModelAttributionDto(
                    ModelId: g.Key.ModelId,
                    ModelName: g.Key.ModelName,
                    TotalRequests: g.Count(),
                    TotalPromptTokens: g.Sum(e => (long)e.PromptTokens),
                    TotalCompletionTokens: g.Sum(e => (long)e.CompletionTokens),
                    EstimatedCostUsd: Math.Round(g.Sum(e => e.EstimatedCostUsd ?? 0m), 4)))
                .OrderByDescending(m => m.EstimatedCostUsd)
                .ToList();

            return Result<Response>.Success(new Response(
                PeriodDays: request.PeriodDays,
                TotalRequests: filtered.Count,
                TotalPromptTokens: filtered.Sum(e => (long)e.PromptTokens),
                TotalCompletionTokens: filtered.Sum(e => (long)e.CompletionTokens),
                TotalEstimatedCostUsd: Math.Round(filtered.Sum(e => e.EstimatedCostUsd ?? 0m), 4),
                ByModel: byModel));
        }
    }

    public sealed record Response(
        int PeriodDays,
        int TotalRequests,
        long TotalPromptTokens,
        long TotalCompletionTokens,
        decimal TotalEstimatedCostUsd,
        IReadOnlyList<ModelAttributionDto> ByModel);

    public sealed record ModelAttributionDto(
        string ModelId,
        string ModelName,
        int TotalRequests,
        long TotalPromptTokens,
        long TotalCompletionTokens,
        decimal EstimatedCostUsd);
}
