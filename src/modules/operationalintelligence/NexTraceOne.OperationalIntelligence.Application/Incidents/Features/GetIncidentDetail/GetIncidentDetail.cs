using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentDetail;

/// <summary>
/// Feature: GetIncidentDetail — retorna o detalhe consolidado de um incidente.
/// Inclui identidade, serviço, owner, severidade, status, timeline, correlação,
/// evidência, mudanças relacionadas, contratos, runbooks e mitigação.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetIncidentDetail
{
    /// <summary>Query para obter o detalhe de um incidente.</summary>
    public sealed record Query(string IncidentId) : IQuery<Response>;

    /// <summary>Valida o identificador do incidente.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que compõe o detalhe consolidado de um incidente.
    /// Simula composição cross-module até integração completa.
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var detail = FindIncidentDetail(request.IncidentId);
            if (detail is null)
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            return Task.FromResult(Result<Response>.Success(detail));
        }

        private static Response? FindIncidentDetail(string incidentId)
        {
            var now = DateTimeOffset.UtcNow;

            // Simulated incident details
            if (incidentId.Equals("a1b2c3d4-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    Identity: new IncidentIdentity(
                        Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001"),
                        "INC-2026-0042",
                        "Payment Gateway — elevated error rate",
                        "Error rate increased to 8.2% after deployment of v2.14.0. Multiple payment flows affected.",
                        IncidentType.ServiceDegradation,
                        IncidentSeverity.Critical,
                        IncidentStatus.Mitigating,
                        now.AddHours(-3),
                        now.AddMinutes(-15)),
                    LinkedServices: new[]
                    {
                        new LinkedServiceItem("svc-payment-gateway", "Payment Gateway", "RestApi", "Critical"),
                        new LinkedServiceItem("svc-order-api", "Order API", "RestApi", "Critical"),
                    },
                    OwnerTeam: "payment-squad",
                    ImpactedDomain: "Payments",
                    ImpactedEnvironment: "Production",
                    Timeline: new[]
                    {
                        new TimelineEntry(now.AddHours(-3), "Incident detected — error rate threshold breached"),
                        new TimelineEntry(now.AddHours(-2.5), "Investigation started — payment-squad notified"),
                        new TimelineEntry(now.AddHours(-2), "Root cause identified — v2.14.0 introduced regression in payment validation"),
                        new TimelineEntry(now.AddHours(-1), "Mitigation started — rollback initiated"),
                        new TimelineEntry(now.AddMinutes(-15), "Rollback deployed — monitoring recovery"),
                    },
                    Correlation: new CorrelationSummary(
                        CorrelationConfidence.High,
                        "Deployment of v2.14.0 strongly correlated with error rate increase. Temporal proximity and blast radius match.",
                        new[]
                        {
                            new RelatedChangeItem(Guid.NewGuid(), "Deploy v2.14.0 to Payment Gateway", "Deployment", "SuspectedRegression", now.AddHours(-4)),
                        },
                        new[]
                        {
                            new RelatedServiceItem("svc-order-api", "Order API", "Downstream payment calls failing"),
                        }),
                    Evidence: new EvidenceSummary(
                        "Error rate: 1.2% → 8.2%. P99 latency: 120ms → 890ms. Timeout rate increased 5x.",
                        "Error rate crossed 5% threshold. P99 latency exceeded SLO. Payment success rate dropped to 91.8%.",
                        new[]
                        {
                            new EvidenceItem("Error rate spike", "Error rate increased from 1.2% to 8.2% within 15 minutes of deployment"),
                            new EvidenceItem("Latency degradation", "P99 latency increased from 120ms to 890ms"),
                            new EvidenceItem("Downstream impact", "Order API reporting payment timeouts"),
                        }),
                    RelatedContracts: new[]
                    {
                        new RelatedContractItem(Guid.NewGuid(), "Payment Processing API", "v2.14.0", "REST", "Active"),
                    },
                    Runbooks: new[]
                    {
                        new RunbookItem("Payment Gateway Rollback Procedure", "https://docs.internal/runbooks/payment-rollback"),
                        new RunbookItem("Payment Error Rate Troubleshooting", "https://docs.internal/runbooks/payment-errors"),
                    },
                    Mitigation: new MitigationSummary(
                        MitigationStatus.InProgress,
                        new[]
                        {
                            new MitigationActionItem("Rollback to v2.13.2", "Applied", true),
                            new MitigationActionItem("Monitor error rate recovery", "In progress", false),
                            new MitigationActionItem("Notify affected downstream teams", "Completed", true),
                        },
                        "Rollback to v2.13.2 is the primary mitigation. Monitoring recovery.",
                        true,
                        "Escalate to payments-lead if error rate does not recover within 30 minutes post-rollback."));
            }

            if (incidentId.Equals("a1b2c3d4-0002-0000-0000-000000000002", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    Identity: new IncidentIdentity(
                        Guid.Parse("a1b2c3d4-0002-0000-0000-000000000002"),
                        "INC-2026-0041",
                        "Catalog Sync — integration partner unreachable",
                        "External catalog provider API returning 503. Product sync stalled.",
                        IncidentType.DependencyFailure,
                        IncidentSeverity.Major,
                        IncidentStatus.Investigating,
                        now.AddHours(-6),
                        now.AddHours(-1)),
                    LinkedServices: new[]
                    {
                        new LinkedServiceItem("svc-catalog-sync", "Catalog Sync", "IntegrationComponent", "Medium"),
                    },
                    OwnerTeam: "platform-squad",
                    ImpactedDomain: "Catalog",
                    ImpactedEnvironment: "Production",
                    Timeline: new[]
                    {
                        new TimelineEntry(now.AddHours(-6), "External API health check failed — 503 responses"),
                        new TimelineEntry(now.AddHours(-5), "Investigation started — platform-squad notified"),
                        new TimelineEntry(now.AddHours(-1), "Vendor contacted — awaiting response"),
                    },
                    Correlation: new CorrelationSummary(
                        CorrelationConfidence.Low,
                        "No internal changes correlated. External dependency failure suspected.",
                        Array.Empty<RelatedChangeItem>(),
                        Array.Empty<RelatedServiceItem>()),
                    Evidence: new EvidenceSummary(
                        "External API returning 503 since 06:00 UTC. Retry queue depth: 1,247.",
                        "Product catalog sync halted. Stale data risk for product listings.",
                        new[]
                        {
                            new EvidenceItem("External API failure", "503 Service Unavailable from catalog-provider.example.com"),
                            new EvidenceItem("Queue buildup", "Sync retry queue depth at 1,247 messages"),
                        }),
                    RelatedContracts: Array.Empty<RelatedContractItem>(),
                    Runbooks: new[]
                    {
                        new RunbookItem("Catalog Sync Manual Recovery", "https://docs.internal/runbooks/catalog-sync-recovery"),
                    },
                    Mitigation: new MitigationSummary(
                        MitigationStatus.NotStarted,
                        Array.Empty<MitigationActionItem>(),
                        "Awaiting vendor response. Manual sync fallback available if needed.",
                        false,
                        "Escalate to platform-lead if vendor does not respond within 2 hours."));
            }

            return null;
        }
    }

    // ── Response records ────────────────────────────────────────────────

    /// <summary>Resposta consolidada do detalhe do incidente.</summary>
    public sealed record Response(
        IncidentIdentity Identity,
        IReadOnlyList<LinkedServiceItem> LinkedServices,
        string OwnerTeam,
        string ImpactedDomain,
        string ImpactedEnvironment,
        IReadOnlyList<TimelineEntry> Timeline,
        CorrelationSummary Correlation,
        EvidenceSummary Evidence,
        IReadOnlyList<RelatedContractItem> RelatedContracts,
        IReadOnlyList<RunbookItem> Runbooks,
        MitigationSummary Mitigation);

    /// <summary>Identidade do incidente.</summary>
    public sealed record IncidentIdentity(
        Guid IncidentId,
        string Reference,
        string Title,
        string Summary,
        IncidentType IncidentType,
        IncidentSeverity Severity,
        IncidentStatus Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    /// <summary>Serviço vinculado ao incidente.</summary>
    public sealed record LinkedServiceItem(
        string ServiceId, string DisplayName, string ServiceType, string Criticality);

    /// <summary>Entrada na timeline do incidente.</summary>
    public sealed record TimelineEntry(DateTimeOffset Timestamp, string Description);

    /// <summary>Resumo de correlação do incidente com mudanças e serviços.</summary>
    public sealed record CorrelationSummary(
        CorrelationConfidence Confidence,
        string Reason,
        IReadOnlyList<RelatedChangeItem> RelatedChanges,
        IReadOnlyList<RelatedServiceItem> RelatedServices);

    /// <summary>Mudança relacionada ao incidente.</summary>
    public sealed record RelatedChangeItem(
        Guid ChangeId, string Description, string ChangeType,
        string ConfidenceStatus, DateTimeOffset DeployedAt);

    /// <summary>Serviço impactado pela correlação.</summary>
    public sealed record RelatedServiceItem(
        string ServiceId, string DisplayName, string ImpactDescription);

    /// <summary>Resumo de evidências do incidente.</summary>
    public sealed record EvidenceSummary(
        string OperationalSignalsSummary,
        string DegradationSummary,
        IReadOnlyList<EvidenceItem> Observations);

    /// <summary>Evidência individual do incidente.</summary>
    public sealed record EvidenceItem(string Title, string Description);

    /// <summary>Contrato relacionado ao incidente.</summary>
    public sealed record RelatedContractItem(
        Guid ContractVersionId, string Name, string Version,
        string Protocol, string LifecycleState);

    /// <summary>Runbook vinculado ao incidente.</summary>
    public sealed record RunbookItem(string Title, string? Url);

    /// <summary>Resumo de mitigação do incidente.</summary>
    public sealed record MitigationSummary(
        MitigationStatus Status,
        IReadOnlyList<MitigationActionItem> Actions,
        string? RollbackGuidance,
        bool RollbackRelevant,
        string? EscalationGuidance);

    /// <summary>Ação de mitigação individual.</summary>
    public sealed record MitigationActionItem(
        string Description, string Status, bool Completed);
}
