using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityDetail;

/// <summary>
/// Feature: GetServiceReliabilityDetail — obtém a visão consolidada de confiabilidade
/// para um serviço específico. Inclui identidade, estado atual, resumo operacional,
/// tendência, mudanças recentes, incidentes, dependências, contratos e runbooks.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetServiceReliabilityDetail
{
    /// <summary>Query para obter detalhe de confiabilidade de um serviço.</summary>
    public sealed record Query(string ServiceId) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que compõe a visão detalhada de confiabilidade de um serviço.
    /// Simula composição cross-module até integração completa.
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Simula dados do serviço para demonstrar a estrutura da resposta.
            // Em produção, compõe dados de Catalog, RuntimeIntelligence,
            // ChangeGovernance e SourceOfTruth via contratos de módulo.

            var response = BuildSimulatedResponse(request.ServiceId);

            if (response is null)
            {
                Result<Response> error = Error.NotFound("Reliability.ServiceNotFound",
                    "Service '{0}' not found", request.ServiceId);
                return Task.FromResult(error);
            }

            return Task.FromResult(Result<Response>.Success(response));
        }

        private static Response? BuildSimulatedResponse(string serviceId)
        {
            return serviceId.ToLowerInvariant() switch
            {
                "svc-order-api" => new Response(
                    Identity: new ServiceIdentity("svc-order-api", "Order API", "RestApi", "Orders", "order-squad", "Critical"),
                    Status: ReliabilityStatus.Healthy,
                    OperationalSummary: "Service operating within expected parameters. All health checks passing.",
                    Trend: new TrendSummary(TrendDirection.Stable, "7d", "All indicators stable over the last 7 days"),
                    Metrics: new OperationalMetrics(99.95m, 45.2m, 0.3m, 1250m, null, null),
                    ActiveFlags: OperationalFlag.None,
                    RecentChanges: [
                        new ChangeSummaryItem(Guid.NewGuid(), "v2.4.1 — Performance tuning", "Deployment", "Validated", DateTimeOffset.UtcNow.AddDays(-3)),
                    ],
                    LinkedIncidents: [],
                    Dependencies: [
                        new DependencySummaryItem("svc-payment-gateway", "Payment Gateway", ReliabilityStatus.Degraded),
                        new DependencySummaryItem("svc-inventory-consumer", "Inventory Consumer", ReliabilityStatus.NeedsAttention),
                    ],
                    LinkedContracts: [
                        new ContractSummaryItem(Guid.NewGuid(), "Order API v2", "2.4.0", "REST", "Published"),
                    ],
                    Runbooks: [
                        new RunbookSummaryItem("Order API — Incident Response", "https://docs.internal/runbooks/order-api"),
                    ],
                    AnomalySummary: "No anomalies detected in the last 24 hours.",
                    Coverage: new ReliabilityCoverageIndicators(true, true, true, true, true, true),
                    IsSimulated: true),

                "svc-payment-gateway" => new Response(
                    Identity: new ServiceIdentity("svc-payment-gateway", "Payment Gateway", "RestApi", "Payments", "payment-squad", "Critical"),
                    Status: ReliabilityStatus.Degraded,
                    OperationalSummary: "Elevated error rate (5.2%) detected since last deployment. Latency P99 above threshold.",
                    Trend: new TrendSummary(TrendDirection.Declining, "24h", "Error rate increased 40% after deployment v3.1.0"),
                    Metrics: new OperationalMetrics(94.8m, 320.5m, 5.2m, 890m, null, null),
                    ActiveFlags: OperationalFlag.AnomalyDetected | OperationalFlag.RecentChangeImpact,
                    RecentChanges: [
                        new ChangeSummaryItem(Guid.NewGuid(), "v3.1.0 — New retry logic", "Deployment", "NeedsAttention", DateTimeOffset.UtcNow.AddHours(-6)),
                    ],
                    LinkedIncidents: [],
                    Dependencies: [
                        new DependencySummaryItem("svc-auth-gateway", "Auth Gateway", ReliabilityStatus.Healthy),
                    ],
                    LinkedContracts: [
                        new ContractSummaryItem(Guid.NewGuid(), "Payment API v3", "3.1.0", "REST", "Published"),
                    ],
                    Runbooks: [
                        new RunbookSummaryItem("Payment Gateway — Degradation Playbook", "https://docs.internal/runbooks/payment-gw"),
                    ],
                    AnomalySummary: "Error rate anomaly detected: 5.2% vs expected 1.0%. Correlated with recent change v3.1.0.",
                    Coverage: new ReliabilityCoverageIndicators(true, true, true, true, true, true),
                    IsSimulated: true),

                "svc-catalog-sync" => new Response(
                    Identity: new ServiceIdentity("svc-catalog-sync", "Catalog Sync", "IntegrationComponent", "Catalog", "platform-squad", "Medium"),
                    Status: ReliabilityStatus.Unavailable,
                    OperationalSummary: "Integration partner unreachable. Service unable to complete sync operations.",
                    Trend: new TrendSummary(TrendDirection.Declining, "2h", "Connectivity lost 2 hours ago"),
                    Metrics: new OperationalMetrics(0m, 0m, 100m, 0m, null, null),
                    ActiveFlags: OperationalFlag.IncidentLinked | OperationalFlag.DependencyRisk,
                    RecentChanges: [],
                    LinkedIncidents: [
                        new IncidentSummaryItem(Guid.NewGuid(), "INC-2024-0042", "Integration partner outage", "Open", DateTimeOffset.UtcNow.AddHours(-2)),
                    ],
                    Dependencies: [],
                    LinkedContracts: [],
                    Runbooks: [],
                    AnomalySummary: "Complete service failure — dependency unavailable.",
                    Coverage: new ReliabilityCoverageIndicators(true, false, false, false, false, true),
                    IsSimulated: true),

                _ => null
            };
        }
    }

    /// <summary>Identidade do serviço.</summary>
    public sealed record ServiceIdentity(
        string ServiceId, string DisplayName, string ServiceType,
        string Domain, string TeamName, string Criticality);

    /// <summary>Resumo da tendência de confiabilidade.</summary>
    public sealed record TrendSummary(
        TrendDirection Direction, string Timeframe, string Summary);

    /// <summary>Métricas operacionais resumidas.</summary>
    public sealed record OperationalMetrics(
        decimal AvailabilityPercent, decimal LatencyP99Ms, decimal ErrorRatePercent,
        decimal RequestsPerSecond, decimal? QueueLag, decimal? ProcessingDelay);

    /// <summary>Mudança recente associada ao serviço.</summary>
    public sealed record ChangeSummaryItem(
        Guid ChangeId, string Description, string ChangeType,
        string ConfidenceStatus, DateTimeOffset DeployedAt);

    /// <summary>Incidente associado ao serviço.</summary>
    public sealed record IncidentSummaryItem(
        Guid IncidentId, string Reference, string Title,
        string Status, DateTimeOffset ReportedAt);

    /// <summary>Dependência do serviço com estado de confiabilidade.</summary>
    public sealed record DependencySummaryItem(
        string ServiceId, string DisplayName, ReliabilityStatus Status);

    /// <summary>Contrato associado ao serviço.</summary>
    public sealed record ContractSummaryItem(
        Guid ContractVersionId, string Name, string Version,
        string Protocol, string LifecycleState);

    /// <summary>Runbook associado ao serviço.</summary>
    public sealed record RunbookSummaryItem(string Title, string? Url);

    /// <summary>Indicadores de cobertura operacional.</summary>
    public sealed record ReliabilityCoverageIndicators(
        bool HasOperationalSignals, bool HasRunbook, bool HasOwner,
        bool HasDependenciesMapped, bool HasRecentChangeContext,
        bool HasIncidentLinkage);

    /// <summary>Resposta consolidada de confiabilidade do serviço.</summary>
    public sealed record Response(
        ServiceIdentity Identity,
        ReliabilityStatus Status,
        string OperationalSummary,
        TrendSummary Trend,
        OperationalMetrics Metrics,
        OperationalFlag ActiveFlags,
        IReadOnlyList<ChangeSummaryItem> RecentChanges,
        IReadOnlyList<IncidentSummaryItem> LinkedIncidents,
        IReadOnlyList<DependencySummaryItem> Dependencies,
        IReadOnlyList<ContractSummaryItem> LinkedContracts,
        IReadOnlyList<RunbookSummaryItem> Runbooks,
        string AnomalySummary,
        ReliabilityCoverageIndicators Coverage,
        bool IsSimulated);
}
