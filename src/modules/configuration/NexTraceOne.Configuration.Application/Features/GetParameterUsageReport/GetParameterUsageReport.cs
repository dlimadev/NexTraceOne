using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Application.Features.GetParameterUsageReport;

/// <summary>
/// Feature: GetParameterUsageReport — analisa utilização de parâmetros de configuração.
/// Retorna estatísticas sobre overrides, parâmetros mais/menos alterados, e cobertura.
/// Pilar: Source of Truth, Operational Intelligence.
/// </summary>
public static class GetParameterUsageReport
{
    /// <summary>Query para obter relatório de utilização de parâmetros.</summary>
    public sealed record Query : IQuery<ParameterUsageReportDto>;

    /// <summary>DTO com o relatório de utilização de parâmetros.</summary>
    public sealed record ParameterUsageReportDto(
        int TotalDefinitions,
        int TotalOverrides,
        int DefinitionsWithOverrides,
        int DefinitionsUsingDefault,
        double OverrideCoveragePercent,
        IReadOnlyList<ParameterOverrideSummaryDto> MostOverridden,
        IReadOnlyList<ParameterOverrideSummaryDto> RecentlyChanged,
        IReadOnlyList<ScopeDistributionDto> OverridesByScope);

    /// <summary>DTO para resumo de overrides por parâmetro.</summary>
    public sealed record ParameterOverrideSummaryDto(
        string Key,
        string DisplayName,
        int OverrideCount,
        DateTimeOffset? LastChangedAt);

    /// <summary>DTO para distribuição de overrides por scope.</summary>
    public sealed record ScopeDistributionDto(
        string Scope,
        int Count);

    /// <summary>Handler que recolhe dados das definições, entradas e auditoria.</summary>
    public sealed class Handler(
        IConfigurationDefinitionRepository definitionRepository,
        IConfigurationEntryRepository entryRepository,
        IConfigurationAuditRepository auditRepository)
        : IQueryHandler<Query, ParameterUsageReportDto>
    {
        public async Task<Result<ParameterUsageReportDto>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var definitions = await definitionRepository.GetAllAsync(cancellationToken);
            var totalDefinitions = definitions.Count;

            // Collect all overrides across all definitions
            var overrideSummaries = new List<ParameterOverrideSummaryDto>();
            var scopeCounts = new Dictionary<string, int>();
            var totalOverrides = 0;

            foreach (var definition in definitions)
            {
                var entries = await entryRepository.GetAllByKeyAsync(definition.Key, cancellationToken);
                var overrideCount = entries.Count;
                totalOverrides += overrideCount;

                DateTimeOffset? lastChanged = entries
                    .Select(e => e.UpdatedAt ?? e.CreatedAt)
                    .OrderByDescending(d => d)
                    .FirstOrDefault();

                if (overrideCount > 0)
                {
                    overrideSummaries.Add(new ParameterOverrideSummaryDto(
                        definition.Key,
                        definition.DisplayName,
                        overrideCount,
                        lastChanged));
                }

                foreach (var entry in entries)
                {
                    var scopeName = entry.Scope.ToString();
                    scopeCounts.TryGetValue(scopeName, out var current);
                    scopeCounts[scopeName] = current + 1;
                }
            }

            var definitionsWithOverrides = overrideSummaries.Count;
            var coveragePercent = totalDefinitions > 0
                ? Math.Round((double)definitionsWithOverrides / totalDefinitions * 100, 2)
                : 0;

            var mostOverridden = overrideSummaries
                .OrderByDescending(s => s.OverrideCount)
                .Take(10)
                .ToList();

            var recentlyChanged = overrideSummaries
                .Where(s => s.LastChangedAt.HasValue)
                .OrderByDescending(s => s.LastChangedAt)
                .Take(10)
                .ToList();

            var scopeDistribution = scopeCounts
                .Select(kv => new ScopeDistributionDto(kv.Key, kv.Value))
                .OrderByDescending(s => s.Count)
                .ToList();

            return new ParameterUsageReportDto(
                TotalDefinitions: totalDefinitions,
                TotalOverrides: totalOverrides,
                DefinitionsWithOverrides: definitionsWithOverrides,
                DefinitionsUsingDefault: totalDefinitions - definitionsWithOverrides,
                OverrideCoveragePercent: coveragePercent,
                MostOverridden: mostOverridden,
                RecentlyChanged: recentlyChanged,
                OverridesByScope: scopeDistribution);
        }
    }
}
