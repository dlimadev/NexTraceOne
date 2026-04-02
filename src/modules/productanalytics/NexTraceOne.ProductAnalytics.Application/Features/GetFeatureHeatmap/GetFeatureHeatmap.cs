using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Enums;

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
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (from, to, periodLabel) = ResolveRange(clock.UtcNow, request.Range);

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
                    .Take(5)
                    .Select(f => new FeatureUsageDto(f.Feature, f.Count))
                    .ToArray();

                var intensity = Math.Round((a.TotalActions / (decimal)maxActions) * 100m, 1);
                var adoptionPercent = totalUniqueUsers > 0
                    ? (int)Math.Round((a.UniqueUsers / (decimal)totalUniqueUsers) * 100m)
                    : 0;

                return new HeatmapCellDto(
                    a.Module,
                    ModuleName: ToModuleDisplayName(a.Module),
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

        private static string ToModuleDisplayName(ProductModule module) => module switch
        {
            ProductModule.AiAssistant => "AI Assistant",
            ProductModule.SourceOfTruth => "Source of Truth",
            ProductModule.ChangeIntelligence => "Change Intelligence",
            ProductModule.ContractStudio => "Contract Studio",
            ProductModule.ServiceCatalog => "Service Catalog",
            ProductModule.IntegrationHub => "Integration Hub",
            ProductModule.ExecutiveViews => "Executive Views",
            ProductModule.DeveloperPortal => "Developer Portal",
            _ => module.ToString()
        };
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
