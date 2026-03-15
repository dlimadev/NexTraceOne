using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetExecutiveDrillDown;

/// <summary>
/// Feature: GetExecutiveDrillDown — drill-down executivo para domínio, equipa ou serviço.
/// Fornece visão detalhada com indicadores, serviços críticos, gaps e recomendações de foco.
/// </summary>
public static class GetExecutiveDrillDown
{
    /// <summary>Query de drill-down executivo. Tipo de entidade: domain, team ou service.</summary>
    public sealed record Query(
        string EntityType,
        string EntityId) : IQuery<Response>;

    /// <summary>Handler que computa drill-down executivo detalhado para uma entidade.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var keyIndicators = new List<KeyIndicatorDto>
            {
                new("Reliability Score", "62.0%", TrendDirection.Declining,
                    "Below target of 80%, driven by Order Processor degradation"),
                new("Change Safety", "48.0%", TrendDirection.Declining,
                    "High rollback rate: 3 rollbacks in last 30 days"),
                new("Incident Recurrence", "35.0%", TrendDirection.Stable,
                    "Recurring incidents in Order Processor and Inventory Sync"),
                new("Contract Coverage", "62.5%", TrendDirection.Improving,
                    "5 of 8 services have defined contracts, up from 3 last quarter"),
                new("Runbook Coverage", "25.0%", TrendDirection.Stable,
                    "Only 2 of 8 services have runbooks, critical gap"),
                new("Maturity Index", "41.3%", TrendDirection.Improving,
                    "Slow improvement, still lowest across all domains"),
                new("FinOps Efficiency", "Wasteful", TrendDirection.Declining,
                    "€18,700/month waste from rollbacks and idle compute"),
                new("Dependency Health", "Medium", TrendDirection.Stable,
                    "6 unmapped minor dependencies across 3 services")
            };

            var criticalServices = new List<CriticalServiceDto>
            {
                new("svc-order-processor", "Order Processor", RiskLevel.Critical,
                    "Service degradation with frequent rollbacks and recurring incidents"),
                new("svc-inventory-sync", "Inventory Sync", RiskLevel.Medium,
                    "No contract defined, missing documentation and redundant sync cycles"),
                new("svc-cart-service", "Cart Service", RiskLevel.Medium,
                    "Missing runbook and incomplete dependency mapping"),
                new("svc-pricing-engine", "Pricing Engine", RiskLevel.High,
                    "Outdated contract, no automated change validation")
            };

            var topGaps = new List<GapDto>
            {
                new("Runbook Coverage", RiskLevel.Critical,
                    "6 of 8 services lack operational runbooks for incident response",
                    "Prioritize runbook creation for Order Processor and Pricing Engine as critical services"),
                new("Change Validation", RiskLevel.High,
                    "No automated blast radius analysis; manual deployment process",
                    "Implement automated change validation in CI/CD pipeline with rollback gates"),
                new("Contract Governance", RiskLevel.Medium,
                    "3 services without formal contracts, 2 with outdated versions",
                    "Schedule contract definition sprint with AI-assisted generation for missing contracts"),
                new("Documentation", RiskLevel.Medium,
                    "Sparse documentation, key business flows undocumented",
                    "Adopt documentation-as-code approach and mandate docs for new features"),
                new("AI Governance", RiskLevel.Low,
                    "No AI governance framework, uncontrolled usage of external models",
                    "Define AI usage policy and implement audit trail for AI-assisted operations")
            };

            var recommendedFocus = new List<string>
            {
                "Stabilize Order Processor: address root cause of degradation and implement circuit breakers",
                "Create runbooks for all critical services within 30 days",
                "Implement automated change validation to reduce rollback rate below 5%",
                "Complete contract definitions for remaining 3 services using AI-assisted generation",
                "Establish FinOps review cadence to address €18,700/month waste"
            };

            var response = new Response(
                EntityType: request.EntityType,
                EntityId: request.EntityId,
                EntityName: "Commerce",
                RiskLevel: RiskLevel.Critical,
                MaturityLevel: MaturityLevel.Developing,
                KeyIndicators: keyIndicators,
                CriticalServices: criticalServices,
                TopGaps: topGaps,
                RecommendedFocus: recommendedFocus,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta de drill-down executivo com indicadores, serviços críticos, gaps e foco recomendado.</summary>
    public sealed record Response(
        string EntityType,
        string EntityId,
        string EntityName,
        RiskLevel RiskLevel,
        MaturityLevel MaturityLevel,
        IReadOnlyList<KeyIndicatorDto> KeyIndicators,
        IReadOnlyList<CriticalServiceDto> CriticalServices,
        IReadOnlyList<GapDto> TopGaps,
        IReadOnlyList<string> RecommendedFocus,
        DateTimeOffset GeneratedAt);

    /// <summary>Indicador-chave com valor, tendência e explicação contextual.</summary>
    public sealed record KeyIndicatorDto(
        string Name,
        string Value,
        TrendDirection Trend,
        string Explanation);

    /// <summary>Serviço crítico com nível de risco e problema principal.</summary>
    public sealed record CriticalServiceDto(
        string ServiceId,
        string ServiceName,
        RiskLevel RiskLevel,
        string MainIssue);

    /// <summary>Gap identificado com severidade, descrição e recomendação.</summary>
    public sealed record GapDto(
        string Area,
        RiskLevel Severity,
        string Description,
        string Recommendation);
}
