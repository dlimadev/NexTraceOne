using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Application.Features.GetParameterComplianceSummary;

/// <summary>
/// Feature: GetParameterComplianceSummary — resumo de compliance da parametrização.
/// Verifica cobertura i18n, definições depreciadas, parâmetros sensíveis e cobertura de validação.
/// Pilar: Governance, Operational Consistency.
/// </summary>
public static class GetParameterComplianceSummary
{
    /// <summary>Query para obter resumo de compliance de parametrização.</summary>
    public sealed record Query : IQuery<ParameterComplianceSummaryDto>;

    /// <summary>DTO com o resumo de compliance.</summary>
    public sealed record ParameterComplianceSummaryDto(
        int TotalDefinitions,
        int WithI18nKeys,
        int WithoutI18nKeys,
        double I18nCoveragePercent,
        int DeprecatedCount,
        int SensitiveCount,
        int WithValidationRules,
        int WithoutValidationRules,
        double ValidationCoveragePercent,
        int EditableCount,
        int ReadOnlyCount,
        IReadOnlyList<CategoryComplianceDto> ByCategory,
        IReadOnlyList<string> DeprecatedKeys);

    /// <summary>DTO para compliance por categoria.</summary>
    public sealed record CategoryComplianceDto(
        string Category,
        int Total,
        int WithI18n,
        int Deprecated);

    /// <summary>Handler que analisa todas as definições para gerar o resumo.</summary>
    public sealed class Handler(
        IConfigurationDefinitionRepository definitionRepository)
        : IQueryHandler<Query, ParameterComplianceSummaryDto>
    {
        public async Task<Result<ParameterComplianceSummaryDto>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var definitions = await definitionRepository.GetAllAsync(cancellationToken);

            var withI18n = definitions.Count(d =>
                d.DisplayName.StartsWith("config.", StringComparison.OrdinalIgnoreCase));
            var deprecated = definitions.Where(d => d.IsDeprecated).ToList();
            var sensitive = definitions.Count(d => d.IsSensitive);
            var withValidation = definitions.Count(d =>
                !string.IsNullOrWhiteSpace(d.ValidationRules));
            var editable = definitions.Count(d => d.IsEditable);

            var byCategory = definitions
                .GroupBy(d => d.Category.ToString())
                .Select(g => new CategoryComplianceDto(
                    Category: g.Key,
                    Total: g.Count(),
                    WithI18n: g.Count(d =>
                        d.DisplayName.StartsWith("config.", StringComparison.OrdinalIgnoreCase)),
                    Deprecated: g.Count(d => d.IsDeprecated)))
                .OrderByDescending(c => c.Total)
                .ToList();

            var total = definitions.Count;
            var i18nPercent = total > 0
                ? Math.Round((double)withI18n / total * 100, 2)
                : 0;
            var validationPercent = total > 0
                ? Math.Round((double)withValidation / total * 100, 2)
                : 0;

            return new ParameterComplianceSummaryDto(
                TotalDefinitions: total,
                WithI18nKeys: withI18n,
                WithoutI18nKeys: total - withI18n,
                I18nCoveragePercent: i18nPercent,
                DeprecatedCount: deprecated.Count,
                SensitiveCount: sensitive,
                WithValidationRules: withValidation,
                WithoutValidationRules: total - withValidation,
                ValidationCoveragePercent: validationPercent,
                EditableCount: editable,
                ReadOnlyCount: total - editable,
                ByCategory: byCategory,
                DeprecatedKeys: deprecated.Select(d => d.Key).ToList());
        }
    }
}
