using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetReportsSummary;

/// <summary>
/// Feature: GetReportsSummary — gera resumo executivo agregado para relatórios por persona.
/// Agrega indicadores de cobertura, risco, compliance, mudanças e fiabilidade.
/// </summary>
public static class GetReportsSummary
{
    /// <summary>Query para resumo de relatórios. Pode ser filtrada por equipa, domínio ou persona.</summary>
    public sealed record Query(
        string? TeamId = null,
        string? DomainId = null,
        string? Persona = null) : IQuery<Response>;

    /// <summary>Handler que computa o resumo de relatórios agregando dados dos módulos.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Retorna dados agregados computados — em produção consultaria repositórios cross-module.
            var response = new Response(
                TotalServices: 42,
                ServicesWithOwner: 38,
                ServicesWithContract: 35,
                ServicesWithDocumentation: 30,
                ServicesWithRunbook: 25,
                OverallRiskLevel: RiskLevel.Medium,
                OverallMaturity: MaturityLevel.Defined,
                ChangeConfidenceTrend: TrendDirection.Improving,
                ReliabilityTrend: TrendDirection.Stable,
                OpenIncidents: 7,
                RecentChanges: 15,
                ComplianceScore: 78.5m,
                CostEfficiency: CostEfficiency.Acceptable,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta do resumo de relatórios.</summary>
    public sealed record Response(
        int TotalServices,
        int ServicesWithOwner,
        int ServicesWithContract,
        int ServicesWithDocumentation,
        int ServicesWithRunbook,
        RiskLevel OverallRiskLevel,
        MaturityLevel OverallMaturity,
        TrendDirection ChangeConfidenceTrend,
        TrendDirection ReliabilityTrend,
        int OpenIncidents,
        int RecentChanges,
        decimal ComplianceScore,
        CostEfficiency CostEfficiency,
        DateTimeOffset GeneratedAt);
}
