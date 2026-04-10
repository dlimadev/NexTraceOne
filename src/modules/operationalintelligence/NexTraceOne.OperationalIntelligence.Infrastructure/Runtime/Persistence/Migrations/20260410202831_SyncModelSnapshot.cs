using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ops_anomaly_narratives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriftFindingId = table.Column<Guid>(type: "uuid", nullable: false),
                    NarrativeText = table.Column<string>(type: "text", nullable: false),
                    SymptomsSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    BaselineComparisonSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    ProbableCauseSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    CorrelatedChangesSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    RecommendedActionsSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    SeverityJustificationSection = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    ModelUsed = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_ops_anomaly_narratives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_environment_drift_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEnvironment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetEnvironment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AnalyzedDimensions = table.Column<string>(type: "jsonb", nullable: false),
                    ServiceVersionDrifts = table.Column<string>(type: "jsonb", nullable: true),
                    ConfigurationDrifts = table.Column<string>(type: "jsonb", nullable: true),
                    ContractVersionDrifts = table.Column<string>(type: "jsonb", nullable: true),
                    DependencyDrifts = table.Column<string>(type: "jsonb", nullable: true),
                    PolicyDrifts = table.Column<string>(type: "jsonb", nullable: true),
                    Recommendations = table.Column<string>(type: "jsonb", nullable: true),
                    TotalDriftItems = table.Column<int>(type: "integer", nullable: false),
                    CriticalDriftItems = table.Column<int>(type: "integer", nullable: false),
                    OverallSeverity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_environment_drift_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_operational_playbooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Steps = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LinkedServiceIds = table.Column<string>(type: "jsonb", nullable: true),
                    LinkedRunbookIds = table.Column<string>(type: "jsonb", nullable: true),
                    Tags = table.Column<string>(type: "jsonb", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeprecatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExecutionCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_operational_playbooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_playbook_executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaybookId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaybookName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExecutedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StepResults = table.Column<string>(type: "jsonb", nullable: true),
                    Evidence = table.Column<string>(type: "jsonb", nullable: true),
                    Notes = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_playbook_executions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_resilience_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChaosExperimentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExperimentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResilienceScore = table.Column<int>(type: "integer", nullable: false),
                    TheoreticalBlastRadius = table.Column<string>(type: "jsonb", nullable: true),
                    ActualBlastRadius = table.Column<string>(type: "jsonb", nullable: true),
                    BlastRadiusDeviation = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    TelemetryObservations = table.Column<string>(type: "jsonb", nullable: true),
                    LatencyImpactMs = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    ErrorRateImpact = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    RecoveryTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    Strengths = table.Column<string>(type: "jsonb", nullable: true),
                    Weaknesses = table.Column<string>(type: "jsonb", nullable: true),
                    Recommendations = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_resilience_reports", x => x.Id);
                    table.CheckConstraint("CK_ops_resilience_reports_score", "\"ResilienceScore\" >= 0 AND \"ResilienceScore\" <= 100");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ops_anomaly_narratives_DriftFindingId",
                table: "ops_anomaly_narratives",
                column: "DriftFindingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ops_anomaly_narratives_tenant_id",
                table: "ops_anomaly_narratives",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_environment_drift_reports_SourceEnvironment_TargetEnvir~",
                table: "ops_environment_drift_reports",
                columns: new[] { "SourceEnvironment", "TargetEnvironment", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_environment_drift_reports_Status",
                table: "ops_environment_drift_reports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_ops_environment_drift_reports_tenant_id",
                table: "ops_environment_drift_reports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_ops_operational_playbooks_tenant_id",
                table: "ops_operational_playbooks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_ops_operational_playbooks_tenant_status",
                table: "ops_operational_playbooks",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_ops_playbook_executions_playbook_id",
                table: "ops_playbook_executions",
                column: "PlaybookId");

            migrationBuilder.CreateIndex(
                name: "ix_ops_playbook_executions_playbook_started",
                table: "ops_playbook_executions",
                columns: new[] { "PlaybookId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_ops_playbook_executions_tenant_id",
                table: "ops_playbook_executions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_ops_resilience_reports_experiment_id",
                table: "ops_resilience_reports",
                column: "ChaosExperimentId");

            migrationBuilder.CreateIndex(
                name: "ix_ops_resilience_reports_status",
                table: "ops_resilience_reports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_ops_resilience_reports_tenant_id",
                table: "ops_resilience_reports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_ops_resilience_reports_tenant_service",
                table: "ops_resilience_reports",
                columns: new[] { "TenantId", "ServiceName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ops_anomaly_narratives");

            migrationBuilder.DropTable(
                name: "ops_environment_drift_reports");

            migrationBuilder.DropTable(
                name: "ops_operational_playbooks");

            migrationBuilder.DropTable(
                name: "ops_playbook_executions");

            migrationBuilder.DropTable(
                name: "ops_resilience_reports");
        }
    }
}
