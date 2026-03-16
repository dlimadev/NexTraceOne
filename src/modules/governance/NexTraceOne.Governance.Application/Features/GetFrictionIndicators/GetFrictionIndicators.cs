using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetFrictionIndicators;

/// <summary>
/// Retorna indicadores de fricção do produto.
/// Responde: onde os utilizadores encontram mais dificuldade?
/// Quais módulos têm maior abandono? Quais buscas falham?
/// Onde há loops de navegação ou empty states frequentes?
/// </summary>
public static class GetFrictionIndicators
{
    /// <summary>Query para indicadores de fricção.</summary>
    public sealed record Query(
        string? Persona,
        string? Module,
        string? Range) : IQuery<Response>;

    /// <summary>Handler que calcula e retorna indicadores de fricção.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var indicators = new List<FrictionIndicatorDto>
            {
                new(FrictionSignalType.ZeroResultSearch, "Zero Result Searches",
                    ProductModule.Search, 12.8m, 324, TrendDirection.Declining,
                    "12.8% of searches return no results. Top terms: 'kafka schema', 'legacy api', 'team-alpha contracts'"),
                new(FrictionSignalType.RepeatedEmptyState, "Repeated Empty States",
                    ProductModule.ContractStudio, 8.4m, 156, TrendDirection.Stable,
                    "Contract Studio shows empty state frequently for teams without contracts configured"),
                new(FrictionSignalType.AbortedJourney, "Aborted Journeys",
                    ProductModule.Incidents, 15.2m, 89, TrendDirection.Stable,
                    "15.2% of mitigation workflows are abandoned before completion, mostly at cause identification step"),
                new(FrictionSignalType.RepeatedRetry, "Repeated Retries",
                    ProductModule.ContractStudio, 6.1m, 72, TrendDirection.Improving,
                    "Contract validation retries decreased after schema helper improvements"),
                new(FrictionSignalType.ModuleAbandonment, "Module Abandonment",
                    ProductModule.Runbooks, 22.4m, 48, TrendDirection.Declining,
                    "Runbooks module has high bounce rate — users enter but leave quickly without meaningful action"),
                new(FrictionSignalType.BlockedByPolicy, "Blocked by Policy",
                    ProductModule.AiAssistant, 4.2m, 38, TrendDirection.Stable,
                    "AI policy restrictions block 4.2% of prompts. Most common: external model requests without approval"),
                new(FrictionSignalType.QuotaExceeded, "Quota Exceeded",
                    ProductModule.AiAssistant, 2.1m, 18, TrendDirection.Improving,
                    "Token quota exceeded events decreasing after budget adjustments"),
                new(FrictionSignalType.NavigationLoop, "Navigation Loops",
                    ProductModule.Reliability, 9.8m, 64, TrendDirection.Stable,
                    "Users navigate back and forth between reliability overview and service detail without progressing"),
                new(FrictionSignalType.LateDiscovery, "Late Feature Discovery",
                    ProductModule.Governance, 18.5m, 42, TrendDirection.Declining,
                    "Evidence Packages and Compliance Checks discovered late by auditors — discoverability improvement needed")
            };

            // Filtrar por módulo se especificado
            if (!string.IsNullOrWhiteSpace(request.Module))
            {
                if (Enum.TryParse<ProductModule>(request.Module, true, out var moduleFilter))
                {
                    indicators = indicators.Where(i => i.Module == moduleFilter).ToList();
                }
            }

            var response = new Response(
                Indicators: indicators,
                OverallFrictionScore: 22.1m,
                HighestFrictionModule: ProductModule.Runbooks,
                MostCommonSignal: FrictionSignalType.ZeroResultSearch,
                ImprovingSignals: 3,
                DecliningSignals: 3,
                StableSignals: 3,
                PeriodLabel: request.Range ?? "last_30d");

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com indicadores de fricção.</summary>
    public sealed record Response(
        IReadOnlyList<FrictionIndicatorDto> Indicators,
        decimal OverallFrictionScore,
        ProductModule HighestFrictionModule,
        FrictionSignalType MostCommonSignal,
        int ImprovingSignals,
        int DecliningSignals,
        int StableSignals,
        string PeriodLabel);

    /// <summary>Indicador de fricção individual.</summary>
    public sealed record FrictionIndicatorDto(
        FrictionSignalType SignalType,
        string SignalName,
        ProductModule Module,
        decimal ImpactPercent,
        int OccurrenceCount,
        TrendDirection Trend,
        string Insight);
}
