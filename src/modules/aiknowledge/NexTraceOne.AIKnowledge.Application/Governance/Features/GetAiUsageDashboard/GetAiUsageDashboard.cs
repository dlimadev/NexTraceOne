using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiUsageDashboard;

// ── DTO público de agregado de uso ─────────────────────────────────────────

/// <summary>
/// Agregado de uso de IA por dimensão.
/// Retornado pelo GetAiUsageDashboard para relatórios de Executive e Platform Admin.
/// </summary>
public sealed record AiUsageAggregate(
    /// <summary>Chave de agrupamento (nome do modelo, ID de utilizador, nome do provider, etc.).</summary>
    string DimensionKey,
    /// <summary>Label legível para a dimensão (ex: nome do modelo, display name do provider).</summary>
    string DimensionLabel,
    /// <summary>Total de tokens consumidos no período.</summary>
    long TotalTokens,
    /// <summary>Total de requests no período.</summary>
    int TotalRequests,
    /// <summary>Custo estimado em USD (null quando CostPerToken não configurado).</summary>
    decimal? EstimatedCostUsd);

// ── Feature VSA ────────────────────────────────────────────────────────────

/// <summary>
/// Feature: GetAiUsageDashboard — agrega uso de IA por dimensão para relatórios de governance.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
///
/// Personas: Executive (resumo por provider/model), Platform Admin (detalhe por user).
/// </summary>
public static class GetAiUsageDashboard
{
    private static readonly HashSet<string> ValidGroupBy =
        new(StringComparer.OrdinalIgnoreCase) { "model", "user", "provider" };

    private static readonly Dictionary<string, int> PeriodToDays = new(StringComparer.OrdinalIgnoreCase)
    {
        ["1d"] = 1,
        ["7d"] = 7,
        ["30d"] = 30,
        ["90d"] = 90,
    };

    public sealed record Query(
        string? Period,
        string? GroupBy,
        int? Top) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Period)
                .Must(p => p is null || PeriodToDays.ContainsKey(p))
                .WithMessage("Period must be one of: 1d, 7d, 30d, 90d.");

            RuleFor(x => x.GroupBy)
                .Must(g => g is null || ValidGroupBy.Contains(g))
                .WithMessage("GroupBy must be one of: model, user, provider.");

            RuleFor(x => x.Top)
                .InclusiveBetween(1, 100)
                .When(x => x.Top.HasValue)
                .WithMessage("Top must be between 1 and 100.");
        }
    }

    public sealed record Response(
        string Period,
        string GroupBy,
        DateTimeOffset From,
        DateTimeOffset To,
        IReadOnlyList<AiUsageAggregate> Items,
        long GrandTotalTokens,
        int GrandTotalRequests,
        decimal? GrandTotalEstimatedCostUsd);

    public sealed class Handler(
        IAiUsageEntryRepository usageRepository,
        ICurrentTenant currentTenant,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var period = request.Period ?? "7d";
            var groupBy = request.GroupBy ?? "model";
            var top = request.Top ?? 20;
            var tenantId = currentTenant.Id;

            var days = PeriodToDays.GetValueOrDefault(period, 7);
            var to = dateTimeProvider.UtcNow;
            var from = to.AddDays(-days);

            var aggregates = await usageRepository.GetAggregatedUsageAsync(
                tenantId, from, to, groupBy, top, cancellationToken);

            var grandTokens = aggregates.Sum(a => a.TotalTokens);
            var grandRequests = aggregates.Sum(a => a.TotalRequests);
            var grandCost = aggregates.Any(a => a.EstimatedCostUsd.HasValue)
                ? aggregates.Sum(a => a.EstimatedCostUsd ?? 0m)
                : (decimal?)null;

            return new Response(
                Period: period,
                GroupBy: groupBy,
                From: from,
                To: to,
                Items: aggregates,
                GrandTotalTokens: grandTokens,
                GrandTotalRequests: grandRequests,
                GrandTotalEstimatedCostUsd: grandCost);
        }
    }
}
