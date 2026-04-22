using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiCostAttributionReport;

/// <summary>
/// Feature: GetAiCostAttributionReport — atribui custo de tokens de IA por dimensão.
/// Agrega por modelo, serviço ou equipa para suportar FinOps contextual por domínio.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class GetAiCostAttributionReport
{
    private static readonly HashSet<string> ValidGroupBy =
        new(StringComparer.OrdinalIgnoreCase) { "model", "service", "team" };

    /// <summary>Query de atribuição de custo de tokens.</summary>
    public sealed record Query(
        Guid? TenantId,
        int PeriodDays = 30,
        string GroupBy = "model") : IQuery<Response>;

    /// <summary>Validador da query de atribuição de custo.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PeriodDays).InclusiveBetween(1, 365);
            RuleFor(x => x.GroupBy)
                .Must(g => ValidGroupBy.Contains(g))
                .WithMessage("GroupBy must be one of: model, service, team.");
        }
    }

    /// <summary>Handler que agrega o custo de tokens por dimensão solicitada.</summary>
    public sealed class Handler(
        IAiTokenUsageLedgerRepository ledgerRepository,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            Guard.Against.Null(request);

            var cutoff = dateTimeProvider.UtcNow.AddDays(-request.PeriodDays);
            var entries = await ledgerRepository.ListByPeriodAsync(cutoff, ct);

            var filtered = entries.AsEnumerable();
            if (request.TenantId.HasValue)
                filtered = filtered.Where(e => e.TenantId == request.TenantId.Value);

            var list = filtered.ToList();
            var totalCost = list.Sum(e => e.EstimatedCostUsd ?? 0m);

            var groupBy = request.GroupBy.ToLowerInvariant();
            var attributions = list
                .GroupBy(e => groupBy switch
                {
                    "model" => string.IsNullOrEmpty(e.ModelName) ? e.ModelId : e.ModelName,
                    "service" => ExtractServiceFromExecutionId(e.ExecutionId),
                    "team" => e.UserId,
                    _ => e.ModelName,
                })
                .Select(g =>
                {
                    var cost = g.Sum(e => e.EstimatedCostUsd ?? 0m);
                    var count = g.Count();
                    return new AttributionEntry(
                        Label: g.Key,
                        TotalTokens: g.Sum(e => (long)e.TotalTokens),
                        CostUsd: cost,
                        RequestCount: count,
                        AvgCostPerRequest: count > 0 ? Math.Round(cost / count, 6) : 0m);
                })
                .OrderByDescending(x => x.CostUsd)
                .ToList();

            return new Response(
                TotalCostUsd: totalCost,
                AttributionEntries: attributions,
                PeriodDays: request.PeriodDays,
                GroupBy: request.GroupBy);
        }

        private static string ExtractServiceFromExecutionId(string executionId)
        {
            // Tenta extrair prefixo de serviço de executionId estruturado (ex: "svc-payments:xxx")
            if (string.IsNullOrEmpty(executionId)) return "Unknown";
            var idx = executionId.IndexOf(':', StringComparison.Ordinal);
            return idx > 0 ? executionId[..idx] : "Unknown";
        }
    }

    /// <summary>Entrada de atribuição de custo por dimensão.</summary>
    public sealed record AttributionEntry(
        string Label,
        long TotalTokens,
        decimal CostUsd,
        int RequestCount,
        decimal AvgCostPerRequest);

    /// <summary>Resposta do relatório de atribuição de custo.</summary>
    public sealed record Response(
        decimal TotalCostUsd,
        IReadOnlyList<AttributionEntry> AttributionEntries,
        int PeriodDays,
        string GroupBy);
}
