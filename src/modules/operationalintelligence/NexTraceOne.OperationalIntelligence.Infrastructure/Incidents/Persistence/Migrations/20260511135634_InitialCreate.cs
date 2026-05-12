using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ops_inc_outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_inc_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_inc_runbook_step_executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunbookId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExecutorUserId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExecutionStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OutputSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ErrorDetail = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_inc_runbook_step_executions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_incident_change_correlations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfidenceLevel = table.Column<int>(type: "integer", nullable: false),
                    MatchType = table.Column<int>(type: "integer", nullable: false),
                    TimeWindowHours = table.Column<int>(type: "integer", nullable: false),
                    CorrelatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChangeDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ChangeEnvironment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChangeOccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    legacy_asset_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    legacy_asset_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    legacy_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_incident_change_correlations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_incident_narratives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    NarrativeText = table.Column<string>(type: "text", nullable: false),
                    SymptomsSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    TimelineSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    ProbableCauseSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    MitigationSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    RelatedChangesSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    AffectedServicesSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    ModelUsed = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastRefreshedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RefreshCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_incident_narratives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerTeam = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ImpactedDomain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HasCorrelation = table.Column<bool>(type: "boolean", nullable: false),
                    CorrelationConfidence = table.Column<int>(type: "integer", nullable: false),
                    MitigationStatus = table.Column<int>(type: "integer", nullable: false),
                    CorrelationAnalysis = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    EvidenceTelemetrySummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    EvidenceBusinessImpact = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    EvidenceAnalysis = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    EvidenceTemporalContext = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    MitigationNarrative = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    HasEscalationPath = table.Column<bool>(type: "boolean", nullable: false),
                    EscalationPath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TimelineJson = table.Column<string>(type: "jsonb", nullable: true),
                    LinkedServicesJson = table.Column<string>(type: "jsonb", nullable: true),
                    CorrelatedChangesJson = table.Column<string>(type: "jsonb", nullable: true),
                    CorrelatedServicesJson = table.Column<string>(type: "jsonb", nullable: true),
                    CorrelatedDependenciesJson = table.Column<string>(type: "jsonb", nullable: true),
                    ImpactedContractsJson = table.Column<string>(type: "jsonb", nullable: true),
                    EvidenceObservationsJson = table.Column<string>(type: "jsonb", nullable: true),
                    RelatedContractsJson = table.Column<string>(type: "jsonb", nullable: true),
                    RunbookLinksJson = table.Column<string>(type: "jsonb", nullable: true),
                    MitigationActionsJson = table.Column<string>(type: "jsonb", nullable: true),
                    MitigationRecommendationsJson = table.Column<string>(type: "jsonb", nullable: true),
                    MitigationRecommendedRunbooksJson = table.Column<string>(type: "jsonb", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    environment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_incidents", x => x.Id);
                    table.CheckConstraint("CK_ops_incidents_severity", "\"Severity\" >= 0 AND \"Severity\" <= 3");
                    table.CheckConstraint("CK_ops_incidents_status", "\"Status\" >= 0 AND \"Status\" <= 5");
                    table.CheckConstraint("CK_ops_incidents_type", "\"Type\" >= 0 AND \"Type\" <= 6");
                });

            migrationBuilder.CreateTable(
                name: "ops_mitigation_validations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ObservedOutcome = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ValidatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChecksJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_mitigation_validations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_mitigation_workflow_actions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    PerformedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PerformedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_mitigation_workflow_actions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_mitigation_workflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    RiskLevel = table.Column<int>(type: "integer", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    LinkedRunbookId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUser = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedOutcome = table.Column<int>(type: "integer", nullable: true),
                    CompletedNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CompletedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StepsJson = table.Column<string>(type: "jsonb", nullable: true),
                    DecisionsJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_mitigation_workflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_post_incident_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentPhase = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RootCauseAnalysis = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    PreventiveActionsJson = table.Column<string>(type: "jsonb", nullable: true),
                    TimelineNarrative = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    ResponsibleTeam = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Facilitator = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Summary = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_post_incident_reviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_runbooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    LinkedService = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LinkedIncidentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StepsJson = table.Column<string>(type: "jsonb", nullable: true),
                    PrerequisitesJson = table.Column<string>(type: "jsonb", nullable: true),
                    PostNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    MaintainedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_runbooks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ops_inc_outbox_messages_CreatedAt",
                table: "ops_inc_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_inc_outbox_messages_IdempotencyKey",
                table: "ops_inc_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_inc_outbox_messages_ProcessedAt",
                table: "ops_inc_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_inc_runbook_step_executions_RunbookId",
                table: "ops_inc_runbook_step_executions",
                column: "RunbookId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_inc_runbook_step_executions_TenantId",
                table: "ops_inc_runbook_step_executions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_ops_icc_incident_change_unique",
                table: "ops_incident_change_correlations",
                columns: new[] { "IncidentId", "ChangeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ops_icc_incident_id",
                table: "ops_incident_change_correlations",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "ix_ops_icc_tenant_id",
                table: "ops_incident_change_correlations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ops_incident_narratives_IncidentId",
                table: "ops_incident_narratives",
                column: "IncidentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ops_incident_narratives_tenant_id",
                table: "ops_incident_narratives",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ops_incidents_DetectedAt",
                table: "ops_incidents",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_incidents_ExternalRef",
                table: "ops_incidents",
                column: "ExternalRef",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_incidents_ServiceId",
                table: "ops_incidents",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_incidents_Severity",
                table: "ops_incidents",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_ops_incidents_Status",
                table: "ops_incidents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_ops_incidents_tenant_environment",
                table: "ops_incidents",
                columns: new[] { "tenant_id", "environment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ops_incidents_tenant_id",
                table: "ops_incidents",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ops_mitigation_validations_IncidentId",
                table: "ops_mitigation_validations",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_mitigation_validations_WorkflowId",
                table: "ops_mitigation_validations",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_mitigation_workflow_actions_IncidentId",
                table: "ops_mitigation_workflow_actions",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_mitigation_workflow_actions_WorkflowId",
                table: "ops_mitigation_workflow_actions",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_mitigation_workflows_IncidentId",
                table: "ops_mitigation_workflows",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_mitigation_workflows_Status",
                table: "ops_mitigation_workflows",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ops_post_incident_reviews_IncidentId",
                table: "ops_post_incident_reviews",
                column: "IncidentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_runbooks_LinkedService",
                table: "ops_runbooks",
                column: "LinkedService");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ops_inc_outbox_messages");

            migrationBuilder.DropTable(
                name: "ops_inc_runbook_step_executions");

            migrationBuilder.DropTable(
                name: "ops_incident_change_correlations");

            migrationBuilder.DropTable(
                name: "ops_incident_narratives");

            migrationBuilder.DropTable(
                name: "ops_incidents");

            migrationBuilder.DropTable(
                name: "ops_mitigation_validations");

            migrationBuilder.DropTable(
                name: "ops_mitigation_workflow_actions");

            migrationBuilder.DropTable(
                name: "ops_mitigation_workflows");

            migrationBuilder.DropTable(
                name: "ops_post_incident_reviews");

            migrationBuilder.DropTable(
                name: "ops_runbooks");
        }
    }
}
