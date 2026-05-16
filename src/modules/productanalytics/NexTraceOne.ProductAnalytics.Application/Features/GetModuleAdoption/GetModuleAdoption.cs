using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.ConfigurationKeys;
using NexTraceOne.ProductAnalytics.Application.Constants;
using NexTraceOne.ProductAnalytics.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application;

namespace NexTraceOne.ProductAnalytics.Application.Features.GetModuleAdoption;

/// <summary>
/// Retorna métricas de adoção por módulo do produto.
/// Responde: quais módulos são mais usados? Quais têm baixa adoção?
/// Quais capabilities têm uso real versus superficial?
/// </summary>
public static class GetModuleAdoption
{
    /// <summary>Query para métricas de adoção de módulos com paginação.</summary>
    public sealed record Query(
        string? Persona,
        string? TeamId,
        string? Range,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Handler que calcula e retorna adoção por módulo.</summary>
    public sealed class Handler(
        IAnalyticsEventRepository repository,
        IDateTimeProvider clock,
        IConfigurationResolutionService configService) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var maxRangeCfg = await configService.ResolveEffectiveValueAsync(AnalyticsConfigKeys.MaxRangeDays, ConfigurationScope.System, null, cancellationToken);
            var maxRangeDays = int.TryParse(maxRangeCfg?.EffectiveValue, out var mrd) ? mrd : AnalyticsConstants.MaxRangeDays;

            var (from, to, periodLabel) = AnalyticsQueryHelper.ResolveRange(clock.UtcNow, request.Range, maxRangeDays);

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

            var allModules = rows
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
                        ModuleName: AnalyticsQueryHelper.ToModuleDisplayName(r.Module),
                        AdoptionPercent: adoptionPercent,
                        TotalActions: r.TotalActions,
                        UniqueUsers: r.UniqueUsers,
                        DepthScore: Math.Round(depthScore, 1),
                        Trend: TrendDirection.Stable,
                        TopFeatures: topFeatures);
                })
                .OrderByDescending(m => m.TotalActions)
                .ToArray();

            var totalCount = allModules.Length;
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
            var modules = allModules.Skip((page - 1) * pageSize).Take(pageSize).ToArray();

            var overallAdoptionScore = allModules.Length == 0 ? 0m : Math.Round(allModules.Average(m => (decimal)m.AdoptionPercent), 1);

            var mostAdopted = allModules.OrderByDescending(m => m.AdoptionPercent).FirstOrDefault()?.Module ?? ProductModule.Dashboard;
            var leastAdopted = allModules.OrderBy(m => m.AdoptionPercent).FirstOrDefault()?.Module ?? ProductModule.Dashboard;

            var response = new Response(
                Modules: modules,
                OverallAdoptionScore: overallAdoptionScore,
                MostAdopted: mostAdopted,
                LeastAdopted: leastAdopted,
                BiggestGrowth: mostAdopted,
                PeriodLabel: periodLabel,
                Page: page,
                PageSize: pageSize,
                TotalCount: totalCount,
                TotalPages: Math.Max(1, totalPages));

            return response;
        }

    }

    /// <summary>Resposta com adoção por módulo e metadados de paginação.</summary>
    public sealed record Response(
        IReadOnlyList<ModuleAdoptionDto> Modules,
        decimal OverallAdoptionScore,
        ProductModule MostAdopted,
        ProductModule LeastAdopted,
        ProductModule BiggestGrowth,
        string PeriodLabel,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);

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
