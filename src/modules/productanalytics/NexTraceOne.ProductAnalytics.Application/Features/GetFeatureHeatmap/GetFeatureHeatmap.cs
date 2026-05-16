using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Application.ConfigurationKeys;
using NexTraceOne.ProductAnalytics.Application.Constants;
using NexTraceOne.ProductAnalytics.Domain.Enums;
using NexTraceOne.ProductAnalytics.Application;

namespace NexTraceOne.ProductAnalytics.Application.Features.GetFeatureHeatmap;

/// <summary>
/// Retorna mapa de calor de adoção de módulos/funcionalidades.
/// Responde: quais módulos e sub-páginas têm menor adoção por equipa/domínio?
/// Fornece uma visão matricial para identificar gaps de adoção.
/// </summary>
public static class GetFeatureHeatmap
{
    /// <summary>Query para mapa de calor de adoção.</summary>
    public sealed record Query(
        string? Persona,
        string? TeamId,
        string? Range) : IQuery<Response>;

    /// <summary>Handler que calcula heatmap de adoção a partir de dados reais.</summary>
    public sealed class Handler(
        IAnalyticsEventRepository repository,
        IDateTimeProvider clock,
        IConfigurationResolutionService configService) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var maxRangeCfg = await configService.ResolveEffectiveValueAsync(AnalyticsConfigKeys.MaxRangeDays, ConfigurationScope.System, null, cancellationToken);
            var maxRangeDays = int.TryParse(maxRangeCfg?.EffectiveValue, out var mrd) ? mrd : AnalyticsConstants.MaxRangeDays;

            var topFeaturesCfg = await configService.ResolveEffectiveValueAsync(AnalyticsConfigKeys.TopFeaturesLimit, ConfigurationScope.System, null, cancellationToken);
            var topFeaturesLimit = int.TryParse(topFeaturesCfg?.EffectiveValue, out var tfl) ? tfl : AnalyticsConstants.TopFeaturesLimit;

            var (from, to, periodLabel) = AnalyticsQueryHelper.ResolveRange(clock.UtcNow, request.Range, maxRangeDays);

            var adoption = await repository.GetModuleAdoptionAsync(
                persona: request.Persona, teamId: request.TeamId, from, to, cancellationToken);

            var featureCounts = await repository.GetFeatureCountsAsync(
                persona: request.Persona, teamId: request.TeamId, from, to, cancellationToken);

            var totalUniqueUsers = await repository.CountUniqueUsersAsync(
                persona: request.Persona, module: null, teamId: request.TeamId, domainId: null,
                from, to, cancellationToken);

            if (adoption.Count == 0)
            {
                return Result<Response>.Success(new Response(
                    Cells: Array.Empty<HeatmapCellDto>(),
                    Modules: Array.Empty<string>(),
                    MaxIntensity: 0m,
                    TotalUniqueUsers: 0,
                    PeriodLabel: periodLabel));
            }

            var maxActions = adoption.Max(a => a.TotalActions);
            if (maxActions <= 0) maxActions = 1;

            var cells = adoption.Select(a =>
            {
                var features = featureCounts
                    .Where(f => f.Module == a.Module)
                    .OrderByDescending(f => f.Count)
                    .Take(topFeaturesLimit)
                    .Select(f => new FeatureUsageDto(f.Feature, f.Count))
                    .ToArray();

                var intensity = Math.Round((a.TotalActions / (decimal)maxActions) * 100m, 1);
                var adoptionPercent = totalUniqueUsers > 0
                    ? (int)Math.Round((a.UniqueUsers / (decimal)totalUniqueUsers) * 100m)
                    : 0;

                return new HeatmapCellDto(
                    a.Module,
                    ModuleName: AnalyticsQueryHelper.ToModuleDisplayName(a.Module),
                    AdoptionPercent: adoptionPercent,
                    TotalActions: a.TotalActions,
                    UniqueUsers: a.UniqueUsers,
                    Intensity: intensity,
                    TopFeatures: features);
            })
            .OrderByDescending(c => c.Intensity)
            .ToArray();

            var modules = cells.Select(c => c.ModuleName).ToArray();
            var maxIntensity = cells.Length > 0 ? cells.Max(c => c.Intensity) : 0m;

            return Result<Response>.Success(new Response(
                Cells: cells,
                Modules: modules,
                MaxIntensity: maxIntensity,
                TotalUniqueUsers: totalUniqueUsers,
                PeriodLabel: periodLabel));
        }

    }

    /// <summary>Resposta com mapa de calor de adoção.</summary>
    public sealed record Response(
        IReadOnlyList<HeatmapCellDto> Cells,
        IReadOnlyList<string> Modules,
        decimal MaxIntensity,
        int TotalUniqueUsers,
        string PeriodLabel);

    /// <summary>Célula do mapa de calor — um módulo com métricas de adoção.</summary>
    public sealed record HeatmapCellDto(
        ProductModule Module,
        string ModuleName,
        int AdoptionPercent,
        long TotalActions,
        int UniqueUsers,
        decimal Intensity,
        IReadOnlyList<FeatureUsageDto> TopFeatures);

    /// <summary>Uso de uma funcionalidade dentro de um módulo.</summary>
    public sealed record FeatureUsageDto(
        string Feature,
        long Count);
}
