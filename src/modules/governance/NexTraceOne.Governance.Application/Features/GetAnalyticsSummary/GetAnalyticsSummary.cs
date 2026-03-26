using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetAnalyticsSummary;

/// <summary>
/// Retorna resumo consolidado de product analytics.
/// Fornece visão de adoção, valor, fricção e tendências do produto.
/// Suporta filtros por persona, módulo, equipa, domínio e período.
/// COMPATIBILIDADE TRANSITÓRIA (P2.4): Handler temporariamente em Governance.Application.
/// Ownership real: módulo Product Analytics. Migração para ProductAnalytics.Application prevista em fase futura.
/// </summary>
public static class GetAnalyticsSummary
{
    /// <summary>Query com filtros opcionais para o resumo de analytics.</summary>
    public sealed record Query(
        string? Persona,
        string? Module,
        string? TeamId,
        string? DomainId,
        string? Range) : IQuery<Response>;

    /// <summary>Handler que compila e retorna o resumo de analytics.</summary>
    public sealed class Handler(
        IAnalyticsEventRepository repository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (from, to, periodLabel) = ResolveRange(clock.UtcNow, request.Range);

            ProductModule? moduleFilter = null;
            if (!string.IsNullOrWhiteSpace(request.Module) && Enum.TryParse<ProductModule>(request.Module, ignoreCase: true, out var moduleValue))
            {
                moduleFilter = moduleValue;
            }

            var totalEvents = await repository.CountAsync(
                persona: request.Persona,
                module: moduleFilter,
                teamId: request.TeamId,
                domainId: request.DomainId,
                from,
                to,
                cancellationToken);

            var uniqueUsers = await repository.CountUniqueUsersAsync(
                persona: request.Persona,
                module: moduleFilter,
                teamId: request.TeamId,
                domainId: request.DomainId,
                from,
                to,
                cancellationToken);

            var activePersonas = await repository.CountActivePersonasAsync(
                module: request.Module,
                teamId: request.TeamId,
                domainId: request.DomainId,
                from,
                to,
                cancellationToken);

            var topModules = await repository.GetTopModulesAsync(
                persona: request.Persona,
                teamId: request.TeamId,
                domainId: request.DomainId,
                from,
                to,
                top: 6,
                cancellationToken);

            var (previousFrom, previousTo, _) = ResolveRange(from, request.Range);
            var previousEvents = await repository.CountAsync(
                persona: request.Persona,
                module: moduleFilter,
                teamId: request.TeamId,
                domainId: request.DomainId,
                from: previousFrom,
                to: previousTo,
                cancellationToken);

            var overallTrend = CompareTrend(totalEvents, previousEvents);
            var adoptionScore = uniqueUsers == 0 ? 0m : Math.Min(100m, totalEvents / (decimal)uniqueUsers);

            var sessionEvents = await repository.ListSessionEventsAsync(
                persona: request.Persona,
                teamId: request.TeamId,
                from,
                to,
                cancellationToken);

            var (valueScore, frictionScore, ttfv, ttcore) = ComputeSessionDerivedMetrics(sessionEvents, totalEvents);

            var response = new Response(
                TotalEvents: totalEvents,
                UniqueUsers: uniqueUsers,
                ActivePersonas: activePersonas,
                TopModules: topModules
                    .Select(m => new ModuleUsageDto(
                        m.Module,
                        ModuleName: ToModuleDisplayName(m.Module),
                        m.EventCount,
                        m.UniqueUsers,
                        Trend: TrendDirection.Stable))
                    .ToArray(),
                AdoptionScore: adoptionScore,
                ValueScore: valueScore,
                FrictionScore: frictionScore,
                AvgTimeToFirstValueMinutes: ttfv,
                AvgTimeToCoreValueMinutes: ttcore,
                TrendDirection: overallTrend,
                PeriodLabel: periodLabel);

            return response;
        }

        private static (DateTimeOffset From, DateTimeOffset To, string Label) ResolveRange(DateTimeOffset utcNow, string? range)
        {
            var label = string.IsNullOrWhiteSpace(range) ? "last_30d" : range;
            var days = label switch
            {
                "last_7d" => 7,
                "last_1d" => 1,
                "last_90d" => 90,
                _ => 30
            };

            return (utcNow.AddDays(-days), utcNow, label);
        }

        private static TrendDirection CompareTrend(long current, long previous)
        {
            if (previous == 0)
                return current == 0 ? TrendDirection.Stable : TrendDirection.Improving;

            var delta = (current - previous) / (decimal)previous;
            if (delta >= 0.05m) return TrendDirection.Improving;
            if (delta <= -0.05m) return TrendDirection.Declining;
            return TrendDirection.Stable;
        }

        private static (decimal ValueScore, decimal FrictionScore, decimal AvgTtfv, decimal AvgTtcore) ComputeSessionDerivedMetrics(
            IReadOnlyList<SessionEventRow> sessionEvents,
            long totalEvents)
        {
            if (totalEvents <= 0 || sessionEvents.Count == 0)
                return (0m, 0m, 0m, 0m);

            var valueEvents = new HashSet<AnalyticsEventType>
            {
                AnalyticsEventType.ContractPublished,
                AnalyticsEventType.OnboardingStepCompleted,
                AnalyticsEventType.AssistantResponseUsed,
                AnalyticsEventType.MitigationWorkflowCompleted
            };

            var coreValueEvents = new HashSet<AnalyticsEventType>
            {
                AnalyticsEventType.ContractPublished,
                AnalyticsEventType.MitigationWorkflowCompleted
            };

            var frictionEvents = new HashSet<AnalyticsEventType>
            {
                AnalyticsEventType.ZeroResultSearch,
                AnalyticsEventType.EmptyStateEncountered,
                AnalyticsEventType.JourneyAbandoned
            };

            var valueCount = sessionEvents.Count(e => valueEvents.Contains(e.EventType));
            var frictionCount = sessionEvents.Count(e => frictionEvents.Contains(e.EventType));

            var ttfvMinutes = new List<decimal>();
            var ttcoreMinutes = new List<decimal>();

            foreach (var group in sessionEvents.GroupBy(e => e.SessionId))
            {
                var first = group.Min(e => e.OccurredAt);
                var firstValue = group.Where(e => valueEvents.Contains(e.EventType)).Select(e => (DateTimeOffset?)e.OccurredAt).Min();
                if (firstValue is not null)
                {
                    ttfvMinutes.Add((decimal)(firstValue.Value - first).TotalMinutes);
                }

                var firstCore = group.Where(e => coreValueEvents.Contains(e.EventType)).Select(e => (DateTimeOffset?)e.OccurredAt).Min();
                if (firstCore is not null)
                {
                    ttcoreMinutes.Add((decimal)(firstCore.Value - first).TotalMinutes);
                }
            }

            var valueScore = Math.Min(100m, (valueCount / (decimal)totalEvents) * 100m);
            var frictionScore = Math.Min(100m, (frictionCount / (decimal)totalEvents) * 100m);

            var avgTtfv = ttfvMinutes.Count == 0 ? 0m : Math.Round(ttfvMinutes.Average(), 1);
            var avgTtcore = ttcoreMinutes.Count == 0 ? 0m : Math.Round(ttcoreMinutes.Average(), 1);

            return (Math.Round(valueScore, 1), Math.Round(frictionScore, 1), avgTtfv, avgTtcore);
        }

        private static string ToModuleDisplayName(ProductModule module)
            => module switch
            {
                ProductModule.AiAssistant => "AI Assistant",
                ProductModule.SourceOfTruth => "Source of Truth",
                ProductModule.ChangeIntelligence => "Change Intelligence",
                ProductModule.ContractStudio => "Contract Studio",
                ProductModule.ServiceCatalog => "Service Catalog",
                ProductModule.IntegrationHub => "Integration Hub",
                ProductModule.ExecutiveViews => "Executive Views",
                _ => module.ToString()
            };
    }

    /// <summary>Resumo consolidado de product analytics.</summary>
    public sealed record Response(
        long TotalEvents,
        int UniqueUsers,
        int ActivePersonas,
        IReadOnlyList<ModuleUsageDto> TopModules,
        decimal AdoptionScore,
        decimal ValueScore,
        decimal FrictionScore,
        decimal AvgTimeToFirstValueMinutes,
        decimal AvgTimeToCoreValueMinutes,
        TrendDirection TrendDirection,
        string PeriodLabel);

    /// <summary>Resumo de uso por módulo.</summary>
    public sealed record ModuleUsageDto(
        ProductModule Module,
        string ModuleName,
        long EventCount,
        int UniqueUsers,
        TrendDirection Trend);
}
