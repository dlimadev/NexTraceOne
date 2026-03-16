using System.Text.Json;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

/// <summary>
/// Dados seed para a migração inicial do subdomínio Incidents.
/// Replica os mesmos 6 incidentes, 3 runbooks e 2 workflows que existiam
/// no InMemoryIncidentStore para garantir paridade funcional.
/// </summary>
internal static class IncidentSeedData
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // ── Fixed GUIDs (matching InMemoryIncidentStore) ─────────────────────

    private static readonly Guid Inc1 = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001");
    private static readonly Guid Inc2 = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000002");
    private static readonly Guid Inc3 = Guid.Parse("a1b2c3d4-0003-0000-0000-000000000003");
    private static readonly Guid Inc4 = Guid.Parse("a1b2c3d4-0004-0000-0000-000000000004");
    private static readonly Guid Inc5 = Guid.Parse("a1b2c3d4-0005-0000-0000-000000000005");
    private static readonly Guid Inc6 = Guid.Parse("a1b2c3d4-0006-0000-0000-000000000006");

    private static readonly Guid Wf1 = Guid.Parse("00000001-0001-0000-0000-000000000001");
    private static readonly Guid Wf2 = Guid.Parse("00000002-0001-0000-0000-000000000001");

    private static readonly Guid Rb1 = Guid.Parse("bb000001-0001-0000-0000-000000000001");
    private static readonly Guid Rb2 = Guid.Parse("bb000002-0001-0000-0000-000000000001");
    private static readonly Guid Rb3 = Guid.Parse("bb000003-0001-0000-0000-000000000001");

    /// <summary>Seed incidents para a migração inicial.</summary>
    public static IReadOnlyList<IncidentRecord> GetIncidents(DateTimeOffset now)
    {
        var incidents = new List<IncidentRecord>();

        // ── INC-1: Payment Gateway ──────────────────────────────────────
        var inc1 = IncidentRecord.Create(
            IncidentRecordId.From(Inc1),
            "INC-2026-0042",
            "Payment Gateway — elevated error rate",
            "Error rate increased to 8.2% after deployment of v2.14.0. Multiple payment flows affected.",
            IncidentType.ServiceDegradation, IncidentSeverity.Critical, IncidentStatus.Mitigating,
            "svc-payment-gateway", "Payment Gateway", "payment-squad",
            "Payments", "Production",
            now.AddHours(-3), now.AddMinutes(-15),
            true, CorrelationConfidence.High, MitigationStatus.InProgress);

        inc1.SetDetail(
            Serialize(new[]
            {
                new TimelineEntry(now.AddHours(-3), "Incident detected — error rate threshold breached"),
                new TimelineEntry(now.AddHours(-2.5), "Investigation started — payment-squad notified"),
                new TimelineEntry(now.AddHours(-2), "Root cause identified — v2.14.0 introduced regression in payment validation"),
                new TimelineEntry(now.AddHours(-1), "Mitigation started — rollback initiated"),
                new TimelineEntry(now.AddMinutes(-15), "Rollback deployed — monitoring recovery"),
            }),
            Serialize(new[]
            {
                new LinkedService("svc-payment-gateway", "Payment Gateway", "RestApi", "Critical"),
                new LinkedService("svc-order-api", "Order API", "RestApi", "Critical"),
            }),
            Serialize(new[] { new RelatedContract(Guid.Parse("dd000001-0001-0000-0000-000000000001"), "Payment Processing API", "v2.14.0", "REST", "Active") }),
            Serialize(new[]
            {
                new RunbookLink("Payment Gateway Rollback Procedure", "https://docs.internal/runbooks/payment-rollback"),
                new RunbookLink("Payment Error Rate Troubleshooting", "https://docs.internal/runbooks/payment-errors"),
            }));

        inc1.SetCorrelation(
            "Deployment of v2.14.0 strongly correlated with error rate increase. Temporal proximity and blast radius match.",
            Serialize(new[] { new CorrelatedChange(Guid.Parse("cc000001-0001-0000-0000-000000000001"), "Deploy v2.14.0 to Payment Gateway", "Deployment", "SuspectedRegression", now.AddHours(-4)) }),
            Serialize(new[]
            {
                new CorrelatedService("svc-payment-gateway", "Payment Gateway", "Primary — source of degradation"),
                new CorrelatedService("svc-order-api", "Order API", "Downstream — payment timeouts affecting orders"),
            }),
            Serialize(new[] { new CorrelatedDependency("svc-order-api", "Order API", "Downstream consumer of payment service") }),
            Serialize(new[] { new ImpactedContract(Guid.Parse("dd000001-0001-0000-0000-000000000001"), "Payment Processing API", "v2.14.0", "REST") }));

        inc1.SetEvidence(
            "Error rate: 1.2% → 8.2%. P99 latency: 120ms → 890ms. Timeout rate increased 5x.",
            "Error rate crossed 5% threshold. P99 latency exceeded SLO. Payment success rate dropped to 91.8%.",
            Serialize(new[]
            {
                new EvidenceObservation("Error rate spike", "Error rate increased from 1.2% to 8.2% within 15 minutes of deployment"),
                new EvidenceObservation("Latency degradation", "P99 latency increased from 120ms to 890ms"),
                new EvidenceObservation("Downstream impact", "Order API reporting payment timeouts"),
                new EvidenceObservation("Temporal correlation", "Metrics degradation started exactly at deployment time of v2.14.0"),
            }),
            "Clear before/after pattern: all key metrics degraded immediately post-deployment.",
            "Deployment window: 09:00–09:15 UTC. Error rate first exceeded threshold at 09:17 UTC.");

        inc1.SetMitigation(
            Serialize(new[]
            {
                new MitigationAction("Rollback to v2.13.2", "Applied", true),
                new MitigationAction("Monitor error rate recovery", "In progress", false),
                new MitigationAction("Notify affected downstream teams", "Completed", true),
            }),
            Serialize(new[]
            {
                new MitigationRunbook("Payment Gateway Rollback Procedure", "https://docs.internal/runbooks/payment-rollback", "Service-specific rollback guide"),
                new MitigationRunbook("Payment Error Rate Troubleshooting", "https://docs.internal/runbooks/payment-errors", "Diagnostic steps for payment errors"),
            }),
            "Rollback to v2.13.2 is the primary mitigation. Monitoring recovery.",
            true,
            "Escalate to payments-lead if error rate does not recover within 30 minutes post-rollback.");

        inc1.SetMitigationRecommendations(Serialize(new[]
        {
            new MitigationRecommendation(
                Guid.Parse("bec00001-0001-0000-0000-000000000001"),
                "Rollback deployment to v2.13.2",
                "Revert the latest deployment that introduced the payment processing regression.",
                MitigationActionType.RollbackCandidate,
                "Deployment v2.14.0 correlates with error rate spike. Previous version was stable.",
                "Error rate increased from 0.1% to 12.4% within 15 minutes of deployment.",
                true, RiskLevel.Medium,
                new[] { Rb1 },
                new[] { "Monitor error rate for 30 minutes post-rollback", "Verify payment success rate returns to baseline" }),
            new MitigationRecommendation(
                Guid.Parse("bec00001-0002-0000-0000-000000000002"),
                "Notify downstream teams of degradation",
                "Alert downstream consumers of the payment service about current issues.",
                MitigationActionType.Escalate,
                "Multiple downstream services depend on payment processing. Early notification reduces blast radius.",
                null, false, RiskLevel.Low,
                Array.Empty<Guid>(),
                new[] { "Confirm notification received by all downstream teams" }),
            new MitigationRecommendation(
                Guid.Parse("bec00001-0003-0000-0000-000000000003"),
                "Investigate contract compatibility impact",
                "Review if the deployment introduced breaking changes in the payment API contract.",
                MitigationActionType.ContractImpactReview,
                "API contract changes in v2.14.0 may affect consumers using the legacy schema.",
                "Schema diff detected between v2.13.2 and v2.14.0 contracts.",
                false, RiskLevel.Low,
                Array.Empty<Guid>(),
                new[] { "Run contract compatibility validation", "Check consumer error logs" }),
        }));

        incidents.Add(inc1);

        // ── INC-2: Catalog Sync ─────────────────────────────────────────
        var inc2 = IncidentRecord.Create(
            IncidentRecordId.From(Inc2),
            "INC-2026-0041",
            "Catalog Sync — integration partner unreachable",
            "External catalog provider API returning 503. Product sync stalled.",
            IncidentType.DependencyFailure, IncidentSeverity.Major, IncidentStatus.Investigating,
            "svc-catalog-sync", "Catalog Sync", "platform-squad",
            "Catalog", "Production",
            now.AddHours(-6), now.AddHours(-1),
            false, CorrelationConfidence.Low, MitigationStatus.NotStarted);

        inc2.SetDetail(
            Serialize(new[]
            {
                new TimelineEntry(now.AddHours(-6), "External API health check failed — 503 responses"),
                new TimelineEntry(now.AddHours(-5), "Investigation started — platform-squad notified"),
                new TimelineEntry(now.AddHours(-1), "Vendor contacted — awaiting response"),
            }),
            Serialize(new[] { new LinkedService("svc-catalog-sync", "Catalog Sync", "IntegrationComponent", "Medium") }),
            Serialize(Array.Empty<RelatedContract>()),
            Serialize(new[] { new RunbookLink("Catalog Sync Manual Recovery", "https://docs.internal/runbooks/catalog-sync-recovery") }));

        inc2.SetCorrelation(
            "No internal changes correlated. External dependency failure suspected.",
            Serialize(Array.Empty<CorrelatedChange>()),
            Serialize(new[] { new CorrelatedService("svc-catalog-sync", "Catalog Sync", "Primary — integration component affected") }),
            Serialize(Array.Empty<CorrelatedDependency>()),
            Serialize(Array.Empty<ImpactedContract>()));

        inc2.SetEvidence(
            "External API returning 503 since 06:00 UTC. Retry queue depth: 1,247.",
            "Product catalog sync halted. Stale data risk for product listings.",
            Serialize(new[]
            {
                new EvidenceObservation("External API failure", "503 Service Unavailable from catalog-provider.example.com"),
                new EvidenceObservation("Queue buildup", "Sync retry queue depth at 1,247 messages"),
            }),
            "External dependency failure — no internal anomalies detected.",
            null);

        inc2.SetMitigation(
            Serialize(new[]
            {
                new MitigationAction("Contact vendor support", "Pending", false),
                new MitigationAction("Enable manual sync fallback", "Available", false),
            }),
            Serialize(new[] { new MitigationRunbook("Catalog Sync Manual Recovery", "https://docs.internal/runbooks/catalog-sync-recovery", "Steps for manual catalog sync") }),
            "Not applicable — external dependency failure.",
            false,
            "Escalate to platform-lead if vendor does not respond within 2 hours.");

        inc2.SetMitigationRecommendations(Serialize(new[]
        {
            new MitigationRecommendation(
                Guid.Parse("bec00002-0001-0000-0000-000000000001"),
                "Verify external dependency health",
                "Check the status of the external catalog sync provider.",
                MitigationActionType.VerifyDependency,
                "External dependency failure detected. Manual verification needed before further action.",
                "Connection timeout errors observed since 14:30 UTC.",
                false, RiskLevel.Low,
                new[] { Rb2 },
                new[] { "Check vendor status page", "Attempt manual sync request" }),
            new MitigationRecommendation(
                Guid.Parse("bec00002-0002-0000-0000-000000000002"),
                "Enable manual sync fallback",
                "Activate the manual sync fallback to maintain catalog availability.",
                MitigationActionType.ExecuteRunbook,
                "Manual fallback can restore partial functionality while external dependency is unavailable.",
                null, true, RiskLevel.Medium,
                new[] { Rb2 },
                new[] { "Verify catalog data freshness after manual sync", "Monitor sync error rate" }),
        }));

        incidents.Add(inc2);

        // ── INC-3 through INC-6 (list-only — no detail) ────────────────
        incidents.Add(IncidentRecord.Create(
            IncidentRecordId.From(Inc3), "INC-2026-0040",
            "Inventory Consumer — consumer lag spike",
            "Kafka consumer lag increasing. Processing backlog growing.",
            IncidentType.MessagingIssue, IncidentSeverity.Major, IncidentStatus.Monitoring,
            "svc-inventory-consumer", "Inventory Consumer", "order-squad",
            "Inventory", "Production",
            now.AddDays(-1), now.AddDays(-1),
            true, CorrelationConfidence.Medium, MitigationStatus.Applied));

        incidents.Add(IncidentRecord.Create(
            IncidentRecordId.From(Inc4), "INC-2026-0039",
            "Order API — latency regression after deploy",
            "P99 latency increased after deployment of new version.",
            IncidentType.OperationalRegression, IncidentSeverity.Minor, IncidentStatus.Resolved,
            "svc-order-api", "Order API", "order-squad",
            "Orders", "Production",
            now.AddDays(-3), now.AddDays(-3),
            true, CorrelationConfidence.Confirmed, MitigationStatus.Verified));

        incidents.Add(IncidentRecord.Create(
            IncidentRecordId.From(Inc5), "INC-2026-0038",
            "Notification Worker — background job failures",
            "Multiple background jobs failing. Queue processing stalled.",
            IncidentType.BackgroundProcessingIssue, IncidentSeverity.Warning, IncidentStatus.Closed,
            "svc-notification-worker", "Notification Worker", "platform-squad",
            "Notifications", "Production",
            now.AddDays(-7), now.AddDays(-7),
            false, CorrelationConfidence.NotAssessed, MitigationStatus.Verified));

        incidents.Add(IncidentRecord.Create(
            IncidentRecordId.From(Inc6), "INC-2026-0037",
            "Auth Gateway — contract schema mismatch",
            "API contract schema mismatch detected after deployment.",
            IncidentType.ContractImpact, IncidentSeverity.Major, IncidentStatus.Resolved,
            "svc-auth-gateway", "Auth Gateway", "identity-squad",
            "Identity", "Staging",
            now.AddDays(-5), now.AddDays(-5),
            true, CorrelationConfidence.High, MitigationStatus.Verified));

        return incidents;
    }

    /// <summary>Seed workflows de mitigação.</summary>
    public static IReadOnlyList<MitigationWorkflowRecord> GetWorkflows()
    {
        var workflows = new List<MitigationWorkflowRecord>();

        // Workflow 1 — INC-1 rollback
        var wf1 = MitigationWorkflowRecord.Create(
            MitigationWorkflowRecordId.From(Wf1),
            Inc1.ToString(),
            "Rollback payment-service to v2.13.2",
            MitigationWorkflowStatus.InProgress,
            MitigationActionType.RollbackCandidate,
            RiskLevel.Medium, true, "ai-assistant",
            Rb1,
            Serialize(new[]
            {
                new WorkflowStep(1, "Trigger rollback pipeline", "Initiate the CI/CD rollback to v2.13.2", true, "ops-engineer@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:36:00Z"), null),
                new WorkflowStep(2, "Validate deployment status", "Confirm rollback deployment completed successfully", true, "ops-engineer@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:42:00Z"), "Deployment verified via health check"),
                new WorkflowStep(3, "Monitor error rate recovery", "Observe error rate for 30 minutes post-rollback", false, null, null, null),
                new WorkflowStep(4, "Confirm resolution", "Verify incident is resolved and close workflow", false, null, null, null),
            }));
        wf1.SetApproval("tech-lead@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:30:00Z"));
        wf1.SetStarted(DateTimeOffset.Parse("2024-06-15T10:35:00Z"));
        wf1.SetDecisions(Serialize(new[]
        {
            new WorkflowDecision(MitigationDecisionType.Approved, "tech-lead@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:30:00Z"), "Approved based on correlation evidence and low risk of rollback."),
        }));
        workflows.Add(wf1);

        // Workflow 2 — INC-2 verify dependency
        var wf2 = MitigationWorkflowRecord.Create(
            MitigationWorkflowRecordId.From(Wf2),
            Inc2.ToString(),
            "Verify external catalog sync dependency",
            MitigationWorkflowStatus.AwaitingApproval,
            MitigationActionType.VerifyDependency,
            RiskLevel.Low, true, "ai-assistant",
            Rb2,
            Serialize(new[]
            {
                new WorkflowStep(1, "Check vendor status page", "Verify current status of external provider", false, null, null, null),
                new WorkflowStep(2, "Attempt manual sync request", "Test connectivity with a manual sync attempt", false, null, null, null),
                new WorkflowStep(3, "Enable fallback mode", "Activate manual sync fallback if vendor is down", false, null, null, null),
            }));
        workflows.Add(wf2);

        return workflows;
    }

    /// <summary>Seed action logs de workflows (histórico de mitigação).</summary>
    public static IReadOnlyList<MitigationWorkflowActionLog> GetWorkflowActionLogs()
    {
        var logs = new List<MitigationWorkflowActionLog>
        {
            // INC-1 history
            MitigationWorkflowActionLog.Create(
                MitigationWorkflowActionLogId.From(Guid.Parse("aad10001-0001-0000-0000-000000000001")),
                Wf1, Inc1.ToString(), "workflow-created", MitigationWorkflowStatus.Draft,
                "ai-assistant", "Workflow created based on AI-generated recommendations.", null,
                DateTimeOffset.Parse("2024-06-15T10:15:00Z")),
            MitigationWorkflowActionLog.Create(
                MitigationWorkflowActionLogId.From(Guid.Parse("aad10001-0002-0000-0000-000000000002")),
                Wf1, Inc1.ToString(), "approved", MitigationWorkflowStatus.Approved,
                "tech-lead@nextraceone.io", "Approved based on correlation evidence and low risk of rollback.", null,
                DateTimeOffset.Parse("2024-06-15T10:30:00Z")),
            MitigationWorkflowActionLog.Create(
                MitigationWorkflowActionLogId.From(Guid.Parse("aad10001-0003-0000-0000-000000000003")),
                Wf1, Inc1.ToString(), "rollback-triggered", MitigationWorkflowStatus.InProgress,
                "ops-engineer@nextraceone.io", "Rollback pipeline triggered for payment-service.", null,
                DateTimeOffset.Parse("2024-06-15T10:36:00Z")),
            MitigationWorkflowActionLog.Create(
                MitigationWorkflowActionLogId.From(Guid.Parse("aad10001-0004-0000-0000-000000000004")),
                Wf1, Inc1.ToString(), "step-completed", MitigationWorkflowStatus.InProgress,
                "ops-engineer@nextraceone.io", "Deployment verified via health check.", null,
                DateTimeOffset.Parse("2024-06-15T10:42:00Z")),
            // INC-2 history
            MitigationWorkflowActionLog.Create(
                MitigationWorkflowActionLogId.From(Guid.Parse("aad10002-0001-0000-0000-000000000001")),
                Wf2, Inc2.ToString(), "workflow-created", MitigationWorkflowStatus.Draft,
                "ai-assistant", "Workflow created for external dependency verification.", null,
                DateTimeOffset.Parse("2024-06-15T14:45:00Z")),
            MitigationWorkflowActionLog.Create(
                MitigationWorkflowActionLogId.From(Guid.Parse("aad10002-0002-0000-0000-000000000002")),
                Guid.Empty, Inc2.ToString(), "recommendation-generated", MitigationWorkflowStatus.Draft,
                "ai-assistant", "AI generated 2 mitigation recommendations for this incident.", null,
                DateTimeOffset.Parse("2024-06-15T14:46:00Z")),
        };
        return logs;
    }

    /// <summary>Seed validation logs.</summary>
    public static IReadOnlyList<MitigationValidationLog> GetValidationLogs()
    {
        var validations = new List<MitigationValidationLog>
        {
            // INC-1 / WF-1 validation (in progress)
            MitigationValidationLog.Create(
                MitigationValidationLogId.From(Guid.Parse("val00001-0001-0000-0000-000000000001")),
                Inc1.ToString(), Wf1, ValidationStatus.InProgress,
                "Error rate recovered to baseline. Payment success rate at 99.7%. Awaiting confirmation from one downstream consumer.",
                null,
                DateTimeOffset.Parse("2024-06-15T11:00:00Z"),
                Serialize(new[]
                {
                    new ValidationCheck("Error rate below threshold", true, "0.3%"),
                    new ValidationCheck("Payment success rate recovered", true, "99.7%"),
                    new ValidationCheck("No new error patterns", true, null),
                    new ValidationCheck("Downstream consumers healthy", false, "2 of 3 confirmed"),
                })),
            // INC-2 / WF-2 validation (pending)
            MitigationValidationLog.Create(
                MitigationValidationLogId.From(Guid.Parse("val00002-0001-0000-0000-000000000001")),
                Inc2.ToString(), Wf2, ValidationStatus.Pending,
                null, null,
                DateTimeOffset.Parse("2024-06-15T15:00:00Z"),
                Serialize(new[]
                {
                    new ValidationCheck("Vendor connectivity restored", false, null),
                    new ValidationCheck("Catalog data freshness", false, null),
                })),
        };
        return validations;
    }

    /// <summary>Seed runbooks.</summary>
    public static IReadOnlyList<RunbookRecord> GetRunbooks()
    {
        return new List<RunbookRecord>
        {
            RunbookRecord.Create(
                RunbookRecordId.From(Rb1),
                "Payment Gateway Rollback Procedure",
                "Step-by-step guide for rolling back the payment-service deployment to a known stable version.",
                "payment-service", "ServiceDegradation",
                Serialize(new[]
                {
                    new RunbookStep(1, "Confirm rollback target version", "Identify the last known stable version from deployment history.", false),
                    new RunbookStep(2, "Notify affected teams", "Send notification to downstream consumers before rollback.", false),
                    new RunbookStep(3, "Trigger rollback pipeline", "Use the CI/CD one-click rollback to deploy the target version.", false),
                    new RunbookStep(4, "Validate deployment health", "Check health endpoints and error rates post-deployment.", false),
                    new RunbookStep(5, "Monitor for 30 minutes", "Observe error rate and payment success metrics for stability.", false),
                    new RunbookStep(6, "Update incident status", "Mark the incident as mitigated and document the outcome.", true),
                }),
                Serialize(new[] { "CI/CD pipeline access for payment-service", "Previous stable version identified", "Downstream teams notified" }),
                "After rollback, monitor error rate and payment success rate for at least 30 minutes. If metrics do not return to baseline, escalate to payments-lead.",
                "platform-team@nextraceone.io",
                DateTimeOffset.Parse("2024-01-15T09:00:00Z"),
                DateTimeOffset.Parse("2024-05-20T14:30:00Z")),

            RunbookRecord.Create(
                RunbookRecordId.From(Rb2),
                "Catalog Sync Manual Recovery",
                "Steps for manually recovering catalog synchronization when the external provider is unavailable.",
                "catalog-service", "DependencyFailure",
                Serialize(new[]
                {
                    new RunbookStep(1, "Check vendor status page", "Verify the current status of the external catalog provider.", false),
                    new RunbookStep(2, "Attempt manual sync request", "Send a manual sync request to test connectivity.", false),
                    new RunbookStep(3, "Enable fallback mode", "Activate the manual sync fallback configuration.", false),
                    new RunbookStep(4, "Verify catalog data freshness", "Confirm catalog data is within acceptable freshness threshold.", false),
                }),
                Serialize(new[] { "Access to catalog-service configuration", "Manual sync endpoint credentials" }),
                "Monitor catalog data freshness and sync error rate. Disable fallback mode once vendor connectivity is restored.",
                "platform-team@nextraceone.io",
                DateTimeOffset.Parse("2024-02-10T11:00:00Z"),
                null),

            RunbookRecord.Create(
                RunbookRecordId.From(Rb3),
                "Generic Service Restart Procedure",
                "Standard procedure for performing a controlled restart of a service with minimal impact.",
                null, null,
                Serialize(new[]
                {
                    new RunbookStep(1, "Notify dependent teams", "Alert teams that depend on this service about the planned restart.", true),
                    new RunbookStep(2, "Drain active connections", "Gracefully drain active connections before restart.", false),
                    new RunbookStep(3, "Trigger controlled restart", "Initiate the restart via orchestrator or deployment tool.", false),
                    new RunbookStep(4, "Verify service health", "Confirm the service is healthy post-restart.", false),
                }),
                Serialize(new[] { "Orchestrator or deployment tool access", "Service health endpoint available" }),
                "Monitor service health and downstream error rates for 15 minutes post-restart.",
                "sre-team@nextraceone.io",
                DateTimeOffset.Parse("2024-03-01T08:00:00Z"),
                DateTimeOffset.Parse("2024-04-10T16:00:00Z")),
        };
    }

    private static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, JsonOpts);

    // ── Internal JSON DTOs for seed data serialization ───────────────────

    private sealed record TimelineEntry(DateTimeOffset Timestamp, string Description);
    private sealed record LinkedService(string ServiceId, string DisplayName, string ServiceType, string Criticality);
    private sealed record CorrelatedChange(Guid ChangeId, string Description, string ChangeType, string ConfidenceStatus, DateTimeOffset DeployedAt);
    private sealed record CorrelatedService(string ServiceId, string DisplayName, string ImpactDescription);
    private sealed record CorrelatedDependency(string ServiceId, string DisplayName, string Relationship);
    private sealed record ImpactedContract(Guid ContractVersionId, string Name, string Version, string Protocol);
    private sealed record EvidenceObservation(string Title, string Description);
    private sealed record RelatedContract(Guid ContractVersionId, string Name, string Version, string Protocol, string LifecycleState);
    private sealed record RunbookLink(string Title, string? Url);
    private sealed record MitigationAction(string Description, string Status, bool Completed);
    private sealed record MitigationRunbook(string Title, string? Url, string? Description);
    private sealed record MitigationRecommendation(
        Guid RecommendationId, string Title, string Summary,
        MitigationActionType RecommendedActionType, string RationaleSummary, string? EvidenceSummary,
        bool RequiresApproval, RiskLevel RiskLevel, Guid[] LinkedRunbookIds, string[] SuggestedValidationSteps);
    private sealed record WorkflowStep(int StepOrder, string Title, string? Description, bool IsCompleted, string? CompletedBy, DateTimeOffset? CompletedAt, string? Notes);
    private sealed record WorkflowDecision(MitigationDecisionType DecisionType, string DecidedBy, DateTimeOffset DecidedAt, string? Reason);
    private sealed record ValidationCheck(string CheckName, bool IsPassed, string? ObservedValue);
    private sealed record RunbookStep(int StepOrder, string Title, string? Description, bool IsOptional);
}
