// DEPRECATED — kept for unit tests only.
// Production registration uses EfIncidentStore (registered in DependencyInjection.cs).
// Do NOT register this class in production DI. This file will be removed when the
// unit test suite migrates to use EfIncidentStore with an in-process test database.
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateMitigationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentCorrelation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentEvidence;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentMitigation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationHistory;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationRecommendations;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationValidation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.RecordMitigationValidation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.UpdateMitigationWorkflowAction;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;

/// <summary>
/// Implementação in-memory do IIncidentStore, centraliza os dados seed
/// que antes estavam espalhados por cada handler.
/// Suporta operações de escrita (criação de workflows, validações) em memória.
/// Será substituído por persistência EF Core em fase futura.
/// </summary>
public sealed class InMemoryIncidentStore : IIncidentStore
{
    // Timestamp de referência — capturado uma vez na construção para manter dados consistentes.
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    // ── IDs fixos para seed data ────────────────────────────────────────
    private const string Inc1 = "a1b2c3d4-0001-0000-0000-000000000001";
    private const string Inc2 = "a1b2c3d4-0002-0000-0000-000000000002";
    private const string Inc3 = "a1b2c3d4-0003-0000-0000-000000000003";
    private const string Inc4 = "a1b2c3d4-0004-0000-0000-000000000004";
    private const string Inc5 = "a1b2c3d4-0005-0000-0000-000000000005";
    private const string Inc6 = "a1b2c3d4-0006-0000-0000-000000000006";

    private const string Wf1 = "00000001-0001-0000-0000-000000000001";
    private const string Wf2 = "00000002-0001-0000-0000-000000000001";

    private const string Rb1 = "bb000001-0001-0000-0000-000000000001";
    private const string Rb2 = "bb000002-0001-0000-0000-000000000001";
    private const string Rb3 = "bb000003-0001-0000-0000-000000000001";

    // Estado mutável (thread-safe via lock simples — suficiente para MVP in-memory)
    private readonly object _lock = new();
    private readonly List<CreatedWorkflow> _createdWorkflows = new();
    private readonly List<CreatedIncident> _createdIncidents = new();
    private readonly List<RecordedValidation> _recordedValidations = new();
    private readonly List<WorkflowAction> _workflowActions = new();

    // ── Incidents ────────────────────────────────────────────────────────

    public CreateIncidentResult CreateIncident(CreateIncidentInput input)
    {
        var incidentId = Guid.NewGuid();
        var detectedAt = input.DetectedAtUtc ?? DateTimeOffset.UtcNow;

        lock (_lock)
        {
            var reference = $"INC-{detectedAt.UtcDateTime.Year}-{(42 + _createdIncidents.Count + 1):0000}";

            _createdIncidents.Add(new CreatedIncident(
                incidentId,
                reference,
                input.Title,
                input.Description,
                input.IncidentType,
                input.Severity,
                input.ServiceId,
                input.ServiceDisplayName,
                input.OwnerTeam,
                input.ImpactedDomain,
                input.Environment,
                detectedAt,
                CorrelationConfidence.NotAssessed,
                0m,
                "No correlated changes computed yet."));

            return new CreateIncidentResult(incidentId, reference, detectedAt);
        }
    }

    public IncidentCorrelationContext? GetIncidentCorrelationContext(string incidentId)
    {
        lock (_lock)
        {
            var created = _createdIncidents.FirstOrDefault(i => i.IncidentId.ToString().Equals(incidentId, StringComparison.OrdinalIgnoreCase));
            if (created is not null)
            {
                return new IncidentCorrelationContext(
                    created.IncidentId,
                    created.ServiceId,
                    created.ServiceDisplayName,
                    created.Environment,
                    created.DetectedAtUtc);
            }
        }

        var listItem = GetIncidentListItems()
            .FirstOrDefault(i => i.IncidentId.ToString().Equals(incidentId, StringComparison.OrdinalIgnoreCase));

        if (listItem is null)
            return null;

        return new IncidentCorrelationContext(
            listItem.IncidentId,
            listItem.ServiceId,
            listItem.ServiceDisplayName,
            listItem.Environment,
            listItem.CreatedAt);
    }

    public void SaveIncidentCorrelation(string incidentId, GetIncidentCorrelation.Response correlation)
    {
        lock (_lock)
        {
            var index = _createdIncidents.FindIndex(i => i.IncidentId.ToString().Equals(incidentId, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                _createdIncidents[index] = _createdIncidents[index] with
                {
                    CorrelationConfidence = correlation.Confidence,
                    CorrelationScore = correlation.Score,
                    CorrelationReason = correlation.Reason,
                };
            }
        }
    }

    public bool IncidentExists(string incidentId) =>
        GetIncidentIds().Contains(incidentId, StringComparer.OrdinalIgnoreCase)
        || _createdIncidents.Any(i => i.IncidentId.ToString().Equals(incidentId, StringComparison.OrdinalIgnoreCase));

    private static HashSet<string> GetIncidentIds() =>
        new(StringComparer.OrdinalIgnoreCase) { Inc1, Inc2, Inc3, Inc4, Inc5, Inc6 };

    public IReadOnlyList<ListIncidents.IncidentListItem> GetIncidentListItems()
    {
        var now = _now;
        var seed = new List<ListIncidents.IncidentListItem>
        {
            new(Guid.Parse(Inc1), "INC-2026-0042", "Payment Gateway — elevated error rate",
                IncidentType.ServiceDegradation, IncidentSeverity.Critical, IncidentStatus.Mitigating,
                "svc-payment-gateway", "Payment Gateway", "payment-squad",
                "Production", now.AddHours(-3), true, CorrelationConfidence.High, MitigationStatus.InProgress),
            new(Guid.Parse(Inc2), "INC-2026-0041", "Catalog Sync — integration partner unreachable",
                IncidentType.DependencyFailure, IncidentSeverity.Major, IncidentStatus.Investigating,
                "svc-catalog-sync", "Catalog Sync", "platform-squad",
                "Production", now.AddHours(-6), false, CorrelationConfidence.Low, MitigationStatus.NotStarted),
            new(Guid.Parse(Inc3), "INC-2026-0040", "Inventory Consumer — consumer lag spike",
                IncidentType.MessagingIssue, IncidentSeverity.Major, IncidentStatus.Monitoring,
                "svc-inventory-consumer", "Inventory Consumer", "order-squad",
                "Production", now.AddDays(-1), true, CorrelationConfidence.Medium, MitigationStatus.Applied),
            new(Guid.Parse(Inc4), "INC-2026-0039", "Order API — latency regression after deploy",
                IncidentType.OperationalRegression, IncidentSeverity.Minor, IncidentStatus.Resolved,
                "svc-order-api", "Order API", "order-squad",
                "Production", now.AddDays(-3), true, CorrelationConfidence.Confirmed, MitigationStatus.Verified),
            new(Guid.Parse(Inc5), "INC-2026-0038", "Notification Worker — background job failures",
                IncidentType.BackgroundProcessingIssue, IncidentSeverity.Warning, IncidentStatus.Closed,
                "svc-notification-worker", "Notification Worker", "platform-squad",
                "Production", now.AddDays(-7), false, CorrelationConfidence.NotAssessed, MitigationStatus.Verified),
            new(Guid.Parse(Inc6), "INC-2026-0037", "Auth Gateway — contract schema mismatch",
                IncidentType.ContractImpact, IncidentSeverity.Major, IncidentStatus.Resolved,
                "svc-auth-gateway", "Auth Gateway", "identity-squad",
                "Staging", now.AddDays(-5), true, CorrelationConfidence.High, MitigationStatus.Verified),
        };

            lock (_lock)
            {
                seed.AddRange(_createdIncidents.Select(i => new ListIncidents.IncidentListItem(
                i.IncidentId,
                i.Reference,
                i.Title,
                i.IncidentType,
                i.Severity,
                IncidentStatus.Open,
                i.ServiceId,
                i.ServiceDisplayName,
                i.OwnerTeam,
                i.Environment,
                i.DetectedAtUtc,
                i.CorrelationConfidence != CorrelationConfidence.NotAssessed,
                i.CorrelationConfidence,
                MitigationStatus.NotStarted)));
            }

            return seed;
    }

    public GetIncidentDetail.Response? GetIncidentDetail(string incidentId)
    {
        var now = _now;
        if (incidentId.Equals(Inc1, StringComparison.OrdinalIgnoreCase))
        {
            return new GetIncidentDetail.Response(
                Identity: new GetIncidentDetail.IncidentIdentity(
                    Guid.Parse(Inc1), "INC-2026-0042",
                    "Payment Gateway — elevated error rate",
                    "Error rate increased to 8.2% after deployment of v2.14.0. Multiple payment flows affected.",
                    IncidentType.ServiceDegradation, IncidentSeverity.Critical, IncidentStatus.Mitigating,
                    now.AddHours(-3), now.AddMinutes(-15)),
                LinkedServices: new[]
                {
                    new GetIncidentDetail.LinkedServiceItem("svc-payment-gateway", "Payment Gateway", "RestApi", "Critical"),
                    new GetIncidentDetail.LinkedServiceItem("svc-order-api", "Order API", "RestApi", "Critical"),
                },
                OwnerTeam: "payment-squad",
                ImpactedDomain: "Payments",
                ImpactedEnvironment: "Production",
                Timeline: new[]
                {
                    new GetIncidentDetail.TimelineEntry(now.AddHours(-3), "Incident detected — error rate threshold breached"),
                    new GetIncidentDetail.TimelineEntry(now.AddHours(-2.5), "Investigation started — payment-squad notified"),
                    new GetIncidentDetail.TimelineEntry(now.AddHours(-2), "Root cause identified — v2.14.0 introduced regression in payment validation"),
                    new GetIncidentDetail.TimelineEntry(now.AddHours(-1), "Mitigation started — rollback initiated"),
                    new GetIncidentDetail.TimelineEntry(now.AddMinutes(-15), "Rollback deployed — monitoring recovery"),
                },
                Correlation: new GetIncidentDetail.CorrelationSummary(
                    CorrelationConfidence.High,
                    "Deployment of v2.14.0 strongly correlated with error rate increase. Temporal proximity and blast radius match.",
                    new[] { new GetIncidentDetail.RelatedChangeItem(Guid.Parse("cc000001-0001-0000-0000-000000000001"), "Deploy v2.14.0 to Payment Gateway", "Deployment", "SuspectedRegression", now.AddHours(-4)) },
                    new[] { new GetIncidentDetail.RelatedServiceItem("svc-order-api", "Order API", "Downstream payment calls failing") }),
                Evidence: new GetIncidentDetail.EvidenceSummary(
                    "Error rate: 1.2% → 8.2%. P99 latency: 120ms → 890ms. Timeout rate increased 5x.",
                    "Error rate crossed 5% threshold. P99 latency exceeded SLO. Payment success rate dropped to 91.8%.",
                    new[]
                    {
                        new GetIncidentDetail.EvidenceItem("Error rate spike", "Error rate increased from 1.2% to 8.2% within 15 minutes of deployment"),
                        new GetIncidentDetail.EvidenceItem("Latency degradation", "P99 latency increased from 120ms to 890ms"),
                        new GetIncidentDetail.EvidenceItem("Downstream impact", "Order API reporting payment timeouts"),
                    }),
                RelatedContracts: new[] { new GetIncidentDetail.RelatedContractItem(Guid.Parse("dd000001-0001-0000-0000-000000000001"), "Payment Processing API", "v2.14.0", "REST", "Active") },
                Runbooks: new[]
                {
                    new GetIncidentDetail.RunbookItem("Payment Gateway Rollback Procedure", "https://docs.internal/runbooks/payment-rollback"),
                    new GetIncidentDetail.RunbookItem("Payment Error Rate Troubleshooting", "https://docs.internal/runbooks/payment-errors"),
                },
                Mitigation: new GetIncidentDetail.MitigationSummary(
                    MitigationStatus.InProgress,
                    new[]
                    {
                        new GetIncidentDetail.MitigationActionItem("Rollback to v2.13.2", "Applied", true),
                        new GetIncidentDetail.MitigationActionItem("Monitor error rate recovery", "In progress", false),
                        new GetIncidentDetail.MitigationActionItem("Notify affected downstream teams", "Completed", true),
                    },
                    "Rollback to v2.13.2 is the primary mitigation. Monitoring recovery.",
                    true,
                    "Escalate to payments-lead if error rate does not recover within 30 minutes post-rollback."));
        }

        if (incidentId.Equals(Inc2, StringComparison.OrdinalIgnoreCase))
        {
            return new GetIncidentDetail.Response(
                Identity: new GetIncidentDetail.IncidentIdentity(
                    Guid.Parse(Inc2), "INC-2026-0041",
                    "Catalog Sync — integration partner unreachable",
                    "External catalog provider API returning 503. Product sync stalled.",
                    IncidentType.DependencyFailure, IncidentSeverity.Major, IncidentStatus.Investigating,
                    now.AddHours(-6), now.AddHours(-1)),
                LinkedServices: new[] { new GetIncidentDetail.LinkedServiceItem("svc-catalog-sync", "Catalog Sync", "IntegrationComponent", "Medium") },
                OwnerTeam: "platform-squad",
                ImpactedDomain: "Catalog",
                ImpactedEnvironment: "Production",
                Timeline: new[]
                {
                    new GetIncidentDetail.TimelineEntry(now.AddHours(-6), "External API health check failed — 503 responses"),
                    new GetIncidentDetail.TimelineEntry(now.AddHours(-5), "Investigation started — platform-squad notified"),
                    new GetIncidentDetail.TimelineEntry(now.AddHours(-1), "Vendor contacted — awaiting response"),
                },
                Correlation: new GetIncidentDetail.CorrelationSummary(
                    CorrelationConfidence.Low,
                    "No internal changes correlated. External dependency failure suspected.",
                    Array.Empty<GetIncidentDetail.RelatedChangeItem>(),
                    Array.Empty<GetIncidentDetail.RelatedServiceItem>()),
                Evidence: new GetIncidentDetail.EvidenceSummary(
                    "External API returning 503 since 06:00 UTC. Retry queue depth: 1,247.",
                    "Product catalog sync halted. Stale data risk for product listings.",
                    new[]
                    {
                        new GetIncidentDetail.EvidenceItem("External API failure", "503 Service Unavailable from catalog-provider.example.com"),
                        new GetIncidentDetail.EvidenceItem("Queue buildup", "Sync retry queue depth at 1,247 messages"),
                    }),
                RelatedContracts: Array.Empty<GetIncidentDetail.RelatedContractItem>(),
                Runbooks: new[] { new GetIncidentDetail.RunbookItem("Catalog Sync Manual Recovery", "https://docs.internal/runbooks/catalog-sync-recovery") },
                Mitigation: new GetIncidentDetail.MitigationSummary(
                    MitigationStatus.NotStarted,
                    Array.Empty<GetIncidentDetail.MitigationActionItem>(),
                    "Awaiting vendor response. Manual sync fallback available if needed.",
                    false,
                    "Escalate to platform-lead if vendor does not respond within 2 hours."));
        }

        return null;
    }

    public GetIncidentCorrelation.Response? GetIncidentCorrelation(string incidentId)
    {
        var now = _now;
        if (incidentId.Equals(Inc1, StringComparison.OrdinalIgnoreCase))
        {
            return new GetIncidentCorrelation.Response(
                Guid.Parse(Inc1), CorrelationConfidence.High,
                82m,
                "Deployment of v2.14.0 strongly correlated with error rate increase. Temporal proximity and blast radius match.",
                new[] { new GetIncidentCorrelation.CorrelatedChange(Guid.Parse("cc000001-0001-0000-0000-000000000001"), "Deploy v2.14.0 to Payment Gateway", "Deployment", "SuspectedRegression", now.AddHours(-4)) },
                new[]
                {
                    new GetIncidentCorrelation.CorrelatedService("svc-payment-gateway", "Payment Gateway", "Primary — source of degradation"),
                    new GetIncidentCorrelation.CorrelatedService("svc-order-api", "Order API", "Downstream — payment timeouts affecting orders"),
                },
                new[] { new GetIncidentCorrelation.CorrelatedDependency("svc-order-api", "Order API", "Downstream consumer of payment service") },
                new[] { new GetIncidentCorrelation.ImpactedContract(Guid.Parse("dd000001-0001-0000-0000-000000000001"), "Payment Processing API", "v2.14.0", "REST") });
        }

        if (incidentId.Equals(Inc2, StringComparison.OrdinalIgnoreCase))
        {
            return new GetIncidentCorrelation.Response(
                Guid.Parse(Inc2), CorrelationConfidence.Low,
                24m,
                "No internal changes correlated. External dependency failure suspected.",
                Array.Empty<GetIncidentCorrelation.CorrelatedChange>(),
                new[] { new GetIncidentCorrelation.CorrelatedService("svc-catalog-sync", "Catalog Sync", "Primary — integration component affected") },
                Array.Empty<GetIncidentCorrelation.CorrelatedDependency>(),
                Array.Empty<GetIncidentCorrelation.ImpactedContract>());
        }

        lock (_lock)
        {
            var created = _createdIncidents.FirstOrDefault(i => i.IncidentId.ToString().Equals(incidentId, StringComparison.OrdinalIgnoreCase));
            if (created is not null)
            {
                return new GetIncidentCorrelation.Response(
                    created.IncidentId,
                    created.CorrelationConfidence,
                    created.CorrelationScore,
                    created.CorrelationReason,
                    [],
                    [new GetIncidentCorrelation.CorrelatedService(created.ServiceId, created.ServiceDisplayName, "Primary impacted service")],
                    [],
                    []);
            }
        }

        return null;
    }

    public GetIncidentEvidence.Response? GetIncidentEvidence(string incidentId)
    {
        if (incidentId.Equals(Inc1, StringComparison.OrdinalIgnoreCase))
        {
            return new GetIncidentEvidence.Response(
                Guid.Parse(Inc1),
                "Error rate: 1.2% → 8.2%. P99 latency: 120ms → 890ms. Timeout rate increased 5x.",
                "Payment success rate dropped from 98.8% to 91.8%. SLO breach confirmed.",
                new[]
                {
                    new GetIncidentEvidence.EvidenceObservation("Error rate spike", "Error rate increased from 1.2% to 8.2% within 15 minutes of deployment"),
                    new GetIncidentEvidence.EvidenceObservation("Latency degradation", "P99 latency increased from 120ms to 890ms"),
                    new GetIncidentEvidence.EvidenceObservation("Downstream impact", "Order API reporting payment timeouts"),
                    new GetIncidentEvidence.EvidenceObservation("Temporal correlation", "Metrics degradation started exactly at deployment time of v2.14.0"),
                },
                "Clear before/after pattern: all key metrics degraded immediately post-deployment.",
                "Deployment window: 09:00–09:15 UTC. Error rate first exceeded threshold at 09:17 UTC.");
        }

        if (incidentId.Equals(Inc2, StringComparison.OrdinalIgnoreCase))
        {
            return new GetIncidentEvidence.Response(
                Guid.Parse(Inc2),
                "External API returning 503 since 06:00 UTC. Retry queue depth: 1,247.",
                "Product catalog sync halted. Stale data risk for product listings.",
                new[]
                {
                    new GetIncidentEvidence.EvidenceObservation("External API failure", "503 Service Unavailable from catalog-provider.example.com"),
                    new GetIncidentEvidence.EvidenceObservation("Queue buildup", "Sync retry queue depth at 1,247 messages"),
                },
                "External dependency failure — no internal anomalies detected.",
                null);
        }

        return null;
    }

    public GetIncidentMitigation.Response? GetIncidentMitigation(string incidentId)
    {
        if (incidentId.Equals(Inc1, StringComparison.OrdinalIgnoreCase))
        {
            return new GetIncidentMitigation.Response(
                Guid.Parse(Inc1), MitigationStatus.InProgress,
                new[]
                {
                    new GetIncidentMitigation.SuggestedAction("Rollback to v2.13.2", "Applied", true),
                    new GetIncidentMitigation.SuggestedAction("Monitor error rate recovery for 30 minutes", "In progress", false),
                    new GetIncidentMitigation.SuggestedAction("Notify affected downstream teams", "Completed", true),
                    new GetIncidentMitigation.SuggestedAction("Create post-incident review ticket", "Pending", false),
                },
                new[]
                {
                    new GetIncidentMitigation.RecommendedRunbook("Payment Gateway Rollback Procedure", "https://docs.internal/runbooks/payment-rollback", "Service-specific rollback guide"),
                    new GetIncidentMitigation.RecommendedRunbook("Payment Error Rate Troubleshooting", "https://docs.internal/runbooks/payment-errors", "Diagnostic steps for payment errors"),
                },
                "Rollback to v2.13.2 is the primary mitigation. Deployment pipeline supports one-click rollback.",
                true,
                "Escalate to payments-lead if error rate does not recover within 30 minutes post-rollback.");
        }

        if (incidentId.Equals(Inc2, StringComparison.OrdinalIgnoreCase))
        {
            return new GetIncidentMitigation.Response(
                Guid.Parse(Inc2), MitigationStatus.NotStarted,
                new[]
                {
                    new GetIncidentMitigation.SuggestedAction("Contact vendor support", "Pending", false),
                    new GetIncidentMitigation.SuggestedAction("Enable manual sync fallback", "Available", false),
                },
                new[] { new GetIncidentMitigation.RecommendedRunbook("Catalog Sync Manual Recovery", "https://docs.internal/runbooks/catalog-sync-recovery", "Steps for manual catalog sync") },
                "Not applicable — external dependency failure.",
                false,
                "Escalate to platform-lead if vendor does not respond within 2 hours.");
        }

        return null;
    }

    // ── Mitigation Workflows ─────────────────────────────────────────────

    public GetMitigationRecommendations.Response? GetMitigationRecommendations(string incidentId)
    {
        if (incidentId.Equals(Inc1, StringComparison.OrdinalIgnoreCase))
        {
            return new GetMitigationRecommendations.Response(
                Guid.Parse(Inc1),
                new[]
                {
                    new GetMitigationRecommendations.MitigationRecommendationDto(
                        Guid.Parse("bec00001-0001-0000-0000-000000000001"), "Rollback deployment to v2.13.2",
                        "Revert the latest deployment that introduced the payment processing regression.",
                        MitigationActionType.RollbackCandidate,
                        "Deployment v2.14.0 correlates with error rate spike. Previous version was stable.",
                        "Error rate increased from 0.1% to 12.4% within 15 minutes of deployment.",
                        true, RiskLevel.Medium,
                        new[] { Guid.Parse(Rb1) },
                        new[] { "Monitor error rate for 30 minutes post-rollback", "Verify payment success rate returns to baseline" }),
                    new GetMitigationRecommendations.MitigationRecommendationDto(
                        Guid.Parse("bec00001-0002-0000-0000-000000000002"), "Notify downstream teams of degradation",
                        "Alert downstream consumers of the payment service about current issues.",
                        MitigationActionType.Escalate,
                        "Multiple downstream services depend on payment processing. Early notification reduces blast radius.",
                        null, false, RiskLevel.Low,
                        Array.Empty<Guid>(),
                        new[] { "Confirm notification received by all downstream teams" }),
                    new GetMitigationRecommendations.MitigationRecommendationDto(
                        Guid.Parse("bec00001-0003-0000-0000-000000000003"), "Investigate contract compatibility impact",
                        "Review if the deployment introduced breaking changes in the payment API contract.",
                        MitigationActionType.ContractImpactReview,
                        "API contract changes in v2.14.0 may affect consumers using the legacy schema.",
                        "Schema diff detected between v2.13.2 and v2.14.0 contracts.",
                        false, RiskLevel.Low,
                        Array.Empty<Guid>(),
                        new[] { "Run contract compatibility validation", "Check consumer error logs" }),
                });
        }

        if (incidentId.Equals(Inc2, StringComparison.OrdinalIgnoreCase))
        {
            return new GetMitigationRecommendations.Response(
                Guid.Parse(Inc2),
                new[]
                {
                    new GetMitigationRecommendations.MitigationRecommendationDto(
                        Guid.Parse("bec00002-0001-0000-0000-000000000001"), "Verify external dependency health",
                        "Check the status of the external catalog sync provider.",
                        MitigationActionType.VerifyDependency,
                        "External dependency failure detected. Manual verification needed before further action.",
                        "Connection timeout errors observed since 14:30 UTC.",
                        false, RiskLevel.Low,
                        new[] { Guid.Parse(Rb2) },
                        new[] { "Check vendor status page", "Attempt manual sync request" }),
                    new GetMitigationRecommendations.MitigationRecommendationDto(
                        Guid.Parse("bec00002-0002-0000-0000-000000000002"), "Enable manual sync fallback",
                        "Activate the manual sync fallback to maintain catalog availability.",
                        MitigationActionType.ExecuteRunbook,
                        "Manual fallback can restore partial functionality while external dependency is unavailable.",
                        null, true, RiskLevel.Medium,
                        new[] { Guid.Parse(Rb2) },
                        new[] { "Verify catalog data freshness after manual sync", "Monitor sync error rate" }),
                });
        }

        return null;
    }

    public GetMitigationWorkflow.Response? GetMitigationWorkflow(string incidentId, string workflowId)
    {
        if (incidentId.Equals(Inc1, StringComparison.OrdinalIgnoreCase)
            && (workflowId.Equals(Wf1, StringComparison.OrdinalIgnoreCase) || workflowId.Equals("wf-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase)))
        {
            return new GetMitigationWorkflow.Response(
                Guid.Parse(Wf1), Guid.Parse(Inc1),
                "Rollback payment-service to v2.13.2",
                MitigationWorkflowStatus.InProgress, MitigationActionType.RollbackCandidate,
                RiskLevel.Medium, true,
                "tech-lead@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:30:00Z"),
                "ai-assistant", DateTimeOffset.Parse("2024-06-15T10:15:00Z"),
                DateTimeOffset.Parse("2024-06-15T10:35:00Z"), null, null, null,
                Guid.Parse(Rb1),
                new[]
                {
                    new GetMitigationWorkflow.WorkflowStepDto(1, "Trigger rollback pipeline", "Initiate the CI/CD rollback to v2.13.2", true, "ops-engineer@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:36:00Z"), null),
                    new GetMitigationWorkflow.WorkflowStepDto(2, "Validate deployment status", "Confirm rollback deployment completed successfully", true, "ops-engineer@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:42:00Z"), "Deployment verified via health check"),
                    new GetMitigationWorkflow.WorkflowStepDto(3, "Monitor error rate recovery", "Observe error rate for 30 minutes post-rollback", false, null, null, null),
                    new GetMitigationWorkflow.WorkflowStepDto(4, "Confirm resolution", "Verify incident is resolved and close workflow", false, null, null, null),
                },
                new[] { new GetMitigationWorkflow.WorkflowDecisionDto(MitigationDecisionType.Approved, "tech-lead@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:30:00Z"), "Approved based on correlation evidence and low risk of rollback.") });
        }

        if (incidentId.Equals(Inc2, StringComparison.OrdinalIgnoreCase)
            && (workflowId.Equals(Wf2, StringComparison.OrdinalIgnoreCase) || workflowId.Equals("wf-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase)))
        {
            return new GetMitigationWorkflow.Response(
                Guid.Parse(Wf2), Guid.Parse(Inc2),
                "Verify external catalog sync dependency",
                MitigationWorkflowStatus.AwaitingApproval, MitigationActionType.VerifyDependency,
                RiskLevel.Low, true,
                null, null,
                "ai-assistant", DateTimeOffset.Parse("2024-06-15T14:45:00Z"),
                null, null, null, null,
                Guid.Parse(Rb2),
                new[]
                {
                    new GetMitigationWorkflow.WorkflowStepDto(1, "Check vendor status page", "Verify current status of external provider", false, null, null, null),
                    new GetMitigationWorkflow.WorkflowStepDto(2, "Attempt manual sync request", "Test connectivity with a manual sync attempt", false, null, null, null),
                    new GetMitigationWorkflow.WorkflowStepDto(3, "Enable fallback mode", "Activate manual sync fallback if vendor is down", false, null, null, null),
                },
                Array.Empty<GetMitigationWorkflow.WorkflowDecisionDto>());
        }

        // Check dynamically created workflows
        lock (_lock)
        {
            var created = _createdWorkflows.FirstOrDefault(w =>
                w.IncidentId.Equals(incidentId, StringComparison.OrdinalIgnoreCase)
                && w.WorkflowId.ToString().Equals(workflowId, StringComparison.OrdinalIgnoreCase));

            if (created is not null)
            {
                return new GetMitigationWorkflow.Response(
                    created.WorkflowId, Guid.Parse(incidentId),
                    created.Title, created.Status, created.ActionType,
                    created.RiskLevel, created.RequiresApproval,
                    null, null, "user", created.CreatedAt,
                    null, null, null, null, created.LinkedRunbookId,
                    created.Steps?.Select((s, i) => new GetMitigationWorkflow.WorkflowStepDto(
                        s.StepOrder, s.Title, s.Description, false, null, null, null)).ToArray()
                    ?? Array.Empty<GetMitigationWorkflow.WorkflowStepDto>(),
                    Array.Empty<GetMitigationWorkflow.WorkflowDecisionDto>());
            }
        }

        return null;
    }

    public CreateMitigationWorkflow.Response CreateMitigationWorkflow(
        string incidentId, string title, MitigationActionType actionType,
        RiskLevel riskLevel, bool requiresApproval, Guid? linkedRunbookId,
        IReadOnlyList<CreateMitigationWorkflow.CreateStepDto>? steps)
    {
        var wfId = Guid.NewGuid();
        var created = new CreatedWorkflow(
            wfId, incidentId, title, MitigationWorkflowStatus.Draft,
            actionType, riskLevel, requiresApproval, linkedRunbookId,
            steps, DateTimeOffset.UtcNow);

        lock (_lock)
        {
            _createdWorkflows.Add(created);
        }

        return new CreateMitigationWorkflow.Response(wfId, MitigationWorkflowStatus.Draft, created.CreatedAt);
    }

    public UpdateMitigationWorkflowAction.Response? UpdateMitigationWorkflowAction(
        string incidentId, string workflowId, string action,
        MitigationWorkflowStatus newStatus, string? performedBy, string? reason, string? notes)
    {
        var wfGuid = Guid.TryParse(workflowId, out var parsed) ? parsed : Guid.NewGuid();

        lock (_lock)
        {
            _workflowActions.Add(new WorkflowAction(
                wfGuid, incidentId, action, newStatus, performedBy, reason, notes, DateTimeOffset.UtcNow));

            // Update status of created workflows
            var created = _createdWorkflows.FirstOrDefault(w =>
                w.WorkflowId == wfGuid && w.IncidentId.Equals(incidentId, StringComparison.OrdinalIgnoreCase));
            if (created is not null)
            {
                _createdWorkflows.Remove(created);
                _createdWorkflows.Add(created with { Status = newStatus });
            }
        }

        return new UpdateMitigationWorkflowAction.Response(wfGuid, newStatus, action, DateTimeOffset.UtcNow);
    }

    public GetMitigationHistory.Response? GetMitigationHistory(string incidentId)
    {
        if (incidentId.Equals(Inc1, StringComparison.OrdinalIgnoreCase))
        {
            return new GetMitigationHistory.Response(
                Guid.Parse(Inc1),
                new[]
                {
                    new GetMitigationHistory.MitigationAuditEntryDto(
                        Guid.Parse("aad10001-0001-0000-0000-000000000001"), Guid.Parse(Wf1),
                        "workflow-created", "ai-assistant", DateTimeOffset.Parse("2024-06-15T10:15:00Z"),
                        "Workflow created based on AI-generated recommendations.", null, null,
                        new[] { "deployment-diff:v2.13.2..v2.14.0", "error-rate-spike:14.2%" }),
                    new GetMitigationHistory.MitigationAuditEntryDto(
                        Guid.Parse("aad10001-0002-0000-0000-000000000002"), Guid.Parse(Wf1),
                        "approved", "tech-lead@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:30:00Z"),
                        "Approved based on correlation evidence and low risk of rollback.", null, null,
                        Array.Empty<string>()),
                    new GetMitigationHistory.MitigationAuditEntryDto(
                        Guid.Parse("aad10001-0003-0000-0000-000000000003"), Guid.Parse(Wf1),
                        "rollback-triggered", "ops-engineer@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:36:00Z"),
                        "Rollback pipeline triggered for payment-service.",
                        MitigationOutcome.Successful, "Deployment reverted successfully",
                        new[] { "pipeline-run:12345" }),
                    new GetMitigationHistory.MitigationAuditEntryDto(
                        Guid.Parse("aad10001-0004-0000-0000-000000000004"), Guid.Parse(Wf1),
                        "step-completed", "ops-engineer@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:42:00Z"),
                        "Deployment verified via health check.", null, null,
                        new[] { "health-check:payment-service:ok" }),
                });
        }

        if (incidentId.Equals(Inc2, StringComparison.OrdinalIgnoreCase))
        {
            return new GetMitigationHistory.Response(
                Guid.Parse(Inc2),
                new[]
                {
                    new GetMitigationHistory.MitigationAuditEntryDto(
                        Guid.Parse("aad10002-0001-0000-0000-000000000001"), Guid.Parse(Wf2),
                        "workflow-created", "ai-assistant", DateTimeOffset.Parse("2024-06-15T14:45:00Z"),
                        "Workflow created for external dependency verification.", null, null,
                        new[] { "connection-timeout:catalog-sync-provider" }),
                    new GetMitigationHistory.MitigationAuditEntryDto(
                        Guid.Parse("aad10002-0002-0000-0000-000000000002"), null,
                        "recommendation-generated", "ai-assistant", DateTimeOffset.Parse("2024-06-15T14:46:00Z"),
                        "AI generated 2 mitigation recommendations for this incident.", null, null,
                        Array.Empty<string>()),
                });
        }

        return null;
    }

    public Task<GetMitigationHistory.Response?> GetMitigationHistoryAsync(
        string incidentId, CancellationToken ct = default)
        => Task.FromResult(GetMitigationHistory(incidentId));

    public GetMitigationValidation.Response? GetMitigationValidation(string incidentId, string workflowId)
    {
        // Check recorded validations first
        lock (_lock)
        {
            var recorded = _recordedValidations.LastOrDefault(v =>
                v.IncidentId.Equals(incidentId, StringComparison.OrdinalIgnoreCase)
                && v.WorkflowId.Equals(workflowId, StringComparison.OrdinalIgnoreCase));

            if (recorded is not null)
            {
                return new GetMitigationValidation.Response(
                    Guid.TryParse(workflowId, out var wfId) ? wfId : Guid.NewGuid(),
                    recorded.Status,
                    recorded.Checks?.Select(c => new GetMitigationValidation.ValidationCheckDto(c.CheckName, null, c.IsPassed, c.ObservedValue)).ToArray()
                        ?? Array.Empty<GetMitigationValidation.ValidationCheckDto>(),
                    recorded.ObservedOutcome,
                    null,
                    recorded.ValidatedAt,
                    recorded.ValidatedBy);
            }
        }

        if (incidentId.Equals(Inc1, StringComparison.OrdinalIgnoreCase)
            && (workflowId.Equals(Wf1, StringComparison.OrdinalIgnoreCase) || workflowId.Equals("wf-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase)))
        {
            return new GetMitigationValidation.Response(
                Guid.Parse(Wf1), ValidationStatus.InProgress,
                new[]
                {
                    new GetMitigationValidation.ValidationCheckDto("Error rate below threshold", "Error rate should return to < 0.5% within 30 minutes", true, "0.3%"),
                    new GetMitigationValidation.ValidationCheckDto("Payment success rate recovered", "Payment success rate should be above 99.5%", true, "99.7%"),
                    new GetMitigationValidation.ValidationCheckDto("No new error patterns", "No new error types should appear post-rollback", true, null),
                    new GetMitigationValidation.ValidationCheckDto("Downstream consumers healthy", "All downstream services report healthy status", false, "2 of 3 confirmed"),
                },
                "Error rate recovered to baseline. Payment success rate at 99.7%. Awaiting confirmation from one downstream consumer.",
                "Overall positive signal. Error rate dropped from 12.4% to 0.3% within 20 minutes. One downstream service still reporting intermittent errors.",
                null, null);
        }

        if (incidentId.Equals(Inc2, StringComparison.OrdinalIgnoreCase)
            && (workflowId.Equals(Wf2, StringComparison.OrdinalIgnoreCase) || workflowId.Equals("wf-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase)))
        {
            return new GetMitigationValidation.Response(
                Guid.Parse(Wf2), ValidationStatus.Pending,
                new[]
                {
                    new GetMitigationValidation.ValidationCheckDto("Vendor connectivity restored", "External provider should respond within timeout", false, null),
                    new GetMitigationValidation.ValidationCheckDto("Catalog data freshness", "Catalog data should be less than 1 hour old", false, null),
                },
                null, null, null, null);
        }

        return null;
    }

    public RecordMitigationValidation.Response? RecordMitigationValidation(
        string incidentId, string workflowId, ValidationStatus status,
        string? observedOutcome, string? validatedBy,
        IReadOnlyList<RecordMitigationValidation.ValidationCheckInput>? checks)
    {
        var wfGuid = Guid.TryParse(workflowId, out var parsed) ? parsed : Guid.NewGuid();
        var validatedAt = DateTimeOffset.UtcNow;

        lock (_lock)
        {
            _recordedValidations.Add(new RecordedValidation(
                incidentId, workflowId, status, observedOutcome, validatedBy, checks, validatedAt));
        }

        return new RecordMitigationValidation.Response(wfGuid, status, validatedAt);
    }

    public Task<RecordMitigationValidation.Response?> RecordMitigationValidationAsync(
        string incidentId, string workflowId, ValidationStatus status,
        string? observedOutcome, string? validatedBy,
        IReadOnlyList<RecordMitigationValidation.ValidationCheckInput>? checks,
        CancellationToken ct = default)
        => Task.FromResult(RecordMitigationValidation(incidentId, workflowId, status, observedOutcome, validatedBy, checks));

    // ── CreateMitigationWorkflow async stub ─────────────────────────────

    public Task<CreateMitigationWorkflow.Response> CreateMitigationWorkflowAsync(
        string incidentId, string title, MitigationActionType actionType,
        RiskLevel riskLevel, bool requiresApproval, Guid? linkedRunbookId,
        IReadOnlyList<CreateMitigationWorkflow.CreateStepDto>? steps,
        CancellationToken ct = default)
        => Task.FromResult(CreateMitigationWorkflow(incidentId, title, actionType, riskLevel, requiresApproval, linkedRunbookId, steps));

    // ── Runbooks — removed: handled by EfRunbookRepository via IRunbookRepository ─────

    // ── Internal state records ───────────────────────────────────────────

    private sealed record CreatedWorkflow(
        Guid WorkflowId, string IncidentId, string Title, MitigationWorkflowStatus Status,
        MitigationActionType ActionType, RiskLevel RiskLevel, bool RequiresApproval,
        Guid? LinkedRunbookId, IReadOnlyList<CreateMitigationWorkflow.CreateStepDto>? Steps,
        DateTimeOffset CreatedAt);

    private sealed record CreatedIncident(
        Guid IncidentId,
        string Reference,
        string Title,
        string Description,
        IncidentType IncidentType,
        IncidentSeverity Severity,
        string ServiceId,
        string ServiceDisplayName,
        string OwnerTeam,
        string? ImpactedDomain,
        string Environment,
        DateTimeOffset DetectedAtUtc,
        CorrelationConfidence CorrelationConfidence,
        decimal CorrelationScore,
        string CorrelationReason);

    private sealed record RecordedValidation(
        string IncidentId, string WorkflowId, ValidationStatus Status,
        string? ObservedOutcome, string? ValidatedBy,
        IReadOnlyList<RecordMitigationValidation.ValidationCheckInput>? Checks,
        DateTimeOffset ValidatedAt);

    private sealed record WorkflowAction(
        Guid WorkflowId, string IncidentId, string Action,
        MitigationWorkflowStatus NewStatus, string? PerformedBy,
        string? Reason, string? Notes, DateTimeOffset PerformedAt);
}
