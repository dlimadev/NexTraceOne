using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialIncidentsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "oi_incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
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
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_incidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oi_mitigation_workflows",
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
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_mitigation_workflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oi_mitigation_workflow_actions",
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
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_mitigation_workflow_actions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oi_mitigation_validations",
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
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_mitigation_validations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oi_runbooks",
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
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_runbooks", x => x.Id);
                });

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS outbox_messages (
                    ""Id"" uuid NOT NULL,
                    ""EventType"" character varying(1000) NOT NULL,
                    ""Payload"" text NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""ProcessedAt"" timestamp with time zone,
                    ""RetryCount"" integer NOT NULL,
                    ""LastError"" character varying(4000),
                    ""TenantId"" uuid NOT NULL,
                    CONSTRAINT ""PK_outbox_messages"" PRIMARY KEY (""Id"")
                );
            ");

            migrationBuilder.CreateIndex(
                name: "IX_oi_incidents_ExternalRef",
                table: "oi_incidents",
                column: "ExternalRef",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oi_incidents_ServiceId",
                table: "oi_incidents",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_oi_incidents_Status",
                table: "oi_incidents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_oi_incidents_Severity",
                table: "oi_incidents",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_oi_incidents_DetectedAt",
                table: "oi_incidents",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_oi_mitigation_workflows_IncidentId",
                table: "oi_mitigation_workflows",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_oi_mitigation_workflows_Status",
                table: "oi_mitigation_workflows",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_oi_mitigation_workflow_actions_WorkflowId",
                table: "oi_mitigation_workflow_actions",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_oi_mitigation_workflow_actions_IncidentId",
                table: "oi_mitigation_workflow_actions",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_oi_mitigation_validations_IncidentId",
                table: "oi_mitigation_validations",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_oi_mitigation_validations_WorkflowId",
                table: "oi_mitigation_validations",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_oi_runbooks_LinkedService",
                table: "oi_runbooks",
                column: "LinkedService");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_outbox_messages_CreatedAt"" ON outbox_messages (""CreatedAt"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_outbox_messages_ProcessedAt"" ON outbox_messages (""ProcessedAt"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "oi_incidents");

            migrationBuilder.DropTable(
                name: "oi_mitigation_workflows");

            migrationBuilder.DropTable(
                name: "oi_mitigation_workflow_actions");

            migrationBuilder.DropTable(
                name: "oi_mitigation_validations");

            migrationBuilder.DropTable(
                name: "oi_runbooks");

            migrationBuilder.Sql(@"DROP TABLE IF EXISTS outbox_messages;");
        }
    }
}
