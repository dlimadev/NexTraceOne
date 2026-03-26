using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetModuleAdoption;

/// <summary>
/// Retorna métricas de adoção por módulo do produto.
/// Responde: quais módulos são mais usados? Quais têm baixa adoção?
/// Quais capabilities têm uso real versus superficial?
/// </summary>
public static class GetModuleAdoption
{
    /// <summary>Query para métricas de adoção de módulos.</summary>
    public sealed record Query(
        string? Persona,
        string? TeamId,
        string? Range) : IQuery<Response>;

    /// <summary>Handler que calcula e retorna adoção por módulo.</summary>
    public sealed class Handler(
        IAnalyticsEventRepository repository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (from, to, periodLabel) = ResolveRange(clock.UtcNow, request.Range);

            var rows = await repository.GetModuleAdoptionAsync(
                persona: request.Persona,
                teamId: request.TeamId,
                from,
                to,
                cancellationToken);

            var totalUniqueUsers = await repository.CountUniqueUsersAsync(
                persona: request.Persona,
                module: null,
                teamId: request.TeamId,
                domainId: null,
                from,
                to,
                cancellationToken);

            var featureCounts = await repository.GetFeatureCountsAsync(
                persona: request.Persona,
                teamId: request.TeamId,
                from,
                to,
                cancellationToken);

            var actionsPerUserByModule = rows
                .Where(r => r.UniqueUsers > 0)
                .Select(r => (Module: r.Module, ActionsPerUser: r.TotalActions / (decimal)r.UniqueUsers))
                .ToDictionary(x => x.Module, x => x.ActionsPerUser);

            var maxActionsPerUser = actionsPerUserByModule.Count == 0 ? 0m : actionsPerUserByModule.Values.Max();

            var modules = rows
                .Select(r =>
                {
                    var adoptionPercent = totalUniqueUsers == 0 ? 0 : (int)Math.Round((r.UniqueUsers / (decimal)totalUniqueUsers) * 100m);
                    var actionsPerUser = actionsPerUserByModule.GetValueOrDefault(r.Module);
                    var depthScore = maxActionsPerUser <= 0m ? 0m : Math.Min(100m, (actionsPerUser / maxActionsPerUser) * 100m);

                    var topFeatures = featureCounts
                        .Where(f => f.Module == r.Module)
                        .OrderByDescending(f => f.Count)
                        .Take(5)
                        .Select(f => f.Feature)
                        .ToArray();

                    return new ModuleAdoptionDto(
                        r.Module,
                        ModuleName: ToModuleDisplayName(r.Module),
                        AdoptionPercent: adoptionPercent,
                        TotalActions: r.TotalActions,
                        UniqueUsers: r.UniqueUsers,
                        DepthScore: Math.Round(depthScore, 1),
                        Trend: TrendDirection.Stable,
                        TopFeatures: topFeatures);
                })
                .OrderByDescending(m => m.TotalActions)
                .ToArray();

            var overallAdoptionScore = modules.Length == 0 ? 0m : Math.Round(modules.Average(m => (decimal)m.AdoptionPercent), 1);

            var mostAdopted = modules.OrderByDescending(m => m.AdoptionPercent).FirstOrDefault()?.Module ?? ProductModule.Dashboard;
            var leastAdopted = modules.OrderBy(m => m.AdoptionPercent).FirstOrDefault()?.Module ?? ProductModule.Dashboard;

            var response = new Response(
                Modules: modules,
                OverallAdoptionScore: overallAdoptionScore,
                MostAdopted: mostAdopted,
                LeastAdopted: leastAdopted,
                BiggestGrowth: mostAdopted,
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

    /// <summary>Resposta com adoção por módulo.</summary>
    public sealed record Response(
        IReadOnlyList<ModuleAdoptionDto> Modules,
        decimal OverallAdoptionScore,
        ProductModule MostAdopted,
        ProductModule LeastAdopted,
        ProductModule BiggestGrowth,
        string PeriodLabel);

    /// <summary>Métricas de adoção de um módulo individual.</summary>
    public sealed record ModuleAdoptionDto(
        ProductModule Module,
        string ModuleName,
        int AdoptionPercent,
        long TotalActions,
        int UniqueUsers,
        decimal DepthScore,
        TrendDirection Trend,
        IReadOnlyList<string> TopFeatures);
}
