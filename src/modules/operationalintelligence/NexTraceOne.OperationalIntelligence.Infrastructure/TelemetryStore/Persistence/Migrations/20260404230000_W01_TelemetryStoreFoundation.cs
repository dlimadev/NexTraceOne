using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class W01_TelemetryStoreFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Service Metrics Snapshots ─────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ops_ts_service_metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AggregationLevel = table.Column<int>(type: "integer", nullable: false),
                    IntervalStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IntervalEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestCount = table.Column<long>(type: "bigint", nullable: false),
                    RequestsPerMinute = table.Column<double>(type: "double precision", nullable: false),
                    RequestsPerHour = table.Column<double>(type: "double precision", nullable: false),
                    ErrorCount = table.Column<long>(type: "bigint", nullable: false),
                    ErrorRatePercent = table.Column<double>(type: "double precision", nullable: false),
                    LatencyAvgMs = table.Column<double>(type: "double precision", nullable: false),
                    LatencyP50Ms = table.Column<double>(type: "double precision", nullable: false),
                    LatencyP95Ms = table.Column<double>(type: "double precision", nullable: false),
                    LatencyP99Ms = table.Column<double>(type: "double precision", nullable: false),
                    LatencyMaxMs = table.Column<double>(type: "double precision", nullable: false),
                    CpuAvgPercent = table.Column<double>(type: "double precision", nullable: true),
                    MemoryAvgMb = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_service_metrics", x => x.Id);
                });

            // ── Dependency Metrics Snapshots ───────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ops_ts_dependency_metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AggregationLevel = table.Column<int>(type: "integer", nullable: false),
                    IntervalStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IntervalEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CallCount = table.Column<long>(type: "bigint", nullable: false),
                    ErrorCount = table.Column<long>(type: "bigint", nullable: false),
                    ErrorRatePercent = table.Column<double>(type: "double precision", nullable: false),
                    LatencyAvgMs = table.Column<double>(type: "double precision", nullable: false),
                    LatencyP95Ms = table.Column<double>(type: "double precision", nullable: false),
                    LatencyP99Ms = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_dependency_metrics", x => x.Id);
                });

            // ── Observed Topology Entries ──────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ops_ts_observed_topology",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CommunicationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TotalCallCount = table.Column<long>(type: "bigint", nullable: false),
                    IsShadowDependency = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_observed_topology", x => x.Id);
                });

            // ── Anomaly Snapshots ──────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ops_ts_anomaly_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnomalyType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MessageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ObservedValue = table.Column<double>(type: "double precision", nullable: false),
                    ExpectedValue = table.Column<double>(type: "double precision", nullable: false),
                    DeviationPercent = table.Column<double>(type: "double precision", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CorrelatedReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_anomaly_snapshots", x => x.Id);
                });

            // ── Telemetry References ───────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ops_ts_telemetry_references",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SignalType = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BackendType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccessUri = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginalTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_telemetry_references", x => x.Id);
                });

            // ── Release Runtime Correlations ───────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ops_ts_release_correlations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeployedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MarkerType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PreDeployErrorRate = table.Column<double>(type: "double precision", nullable: false),
                    PreDeployLatencyP95Ms = table.Column<double>(type: "double precision", nullable: false),
                    PreDeployRequestsPerMinute = table.Column<double>(type: "double precision", nullable: false),
                    PostDeployErrorRate = table.Column<double>(type: "double precision", nullable: false),
                    PostDeployLatencyP95Ms = table.Column<double>(type: "double precision", nullable: false),
                    PostDeployRequestsPerMinute = table.Column<double>(type: "double precision", nullable: false),
                    ImpactScore = table.Column<double>(type: "double precision", nullable: false),
                    ImpactClassification = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TelemetryReferenceIds = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_release_correlations", x => x.Id);
                });

            // ── Investigation Contexts ─────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ops_ts_investigation_contexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TitleMessageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InvestigationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrimaryServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    TimeWindowStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TimeWindowEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AnomalySnapshotIds = table.Column<string>(type: "jsonb", nullable: false),
                    ReleaseCorrelationIds = table.Column<string>(type: "jsonb", nullable: false),
                    TelemetryReferenceIds = table.Column<string>(type: "jsonb", nullable: false),
                    AffectedServiceIds = table.Column<string>(type: "jsonb", nullable: false),
                    AiSummaryJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_ts_investigation_contexts", x => x.Id);
                });

            // ── Outbox Messages ────────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ops_telstore_outbox_messages",
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
                    table.PrimaryKey("PK_ops_telstore_outbox_messages", x => x.Id);
                });

            // ── Indexes ────────────────────────────────────────────────────────────────

            // Service Metrics
            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_service_metrics_service_env_agg_start",
                table: "ops_ts_service_metrics",
                columns: new[] { "ServiceId", "Environment", "AggregationLevel", "IntervalStart" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_service_metrics_name_env_start",
                table: "ops_ts_service_metrics",
                columns: new[] { "ServiceName", "Environment", "IntervalStart" });

            // Dependency Metrics
            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_dependency_metrics_src_tgt_env_agg_start",
                table: "ops_ts_dependency_metrics",
                columns: new[] { "SourceServiceId", "TargetServiceId", "Environment", "AggregationLevel", "IntervalStart" });

            // Observed Topology
            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_observed_topology_src_tgt_env_type",
                table: "ops_ts_observed_topology",
                columns: new[] { "SourceServiceId", "TargetServiceId", "Environment", "CommunicationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_observed_topology_env_shadow",
                table: "ops_ts_observed_topology",
                columns: new[] { "Environment", "IsShadowDependency" });

            // Anomaly Snapshots
            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_anomaly_snapshots_svc_env_detected",
                table: "ops_ts_anomaly_snapshots",
                columns: new[] { "ServiceId", "Environment", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_anomaly_snapshots_severity_resolved",
                table: "ops_ts_anomaly_snapshots",
                columns: new[] { "Severity", "ResolvedAt" });

            // Telemetry References
            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_telemetry_references_correlation",
                table: "ops_ts_telemetry_references",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_telemetry_references_svc_type_ts",
                table: "ops_ts_telemetry_references",
                columns: new[] { "ServiceId", "SignalType", "OriginalTimestamp" });

            // Release Correlations
            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_release_correlations_release",
                table: "ops_ts_release_correlations",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_release_correlations_svc_env_deployed",
                table: "ops_ts_release_correlations",
                columns: new[] { "ServiceId", "Environment", "DeployedAt" });

            // Investigation Contexts
            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_investigation_contexts_svc_env_status",
                table: "ops_ts_investigation_contexts",
                columns: new[] { "PrimaryServiceId", "Environment", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_investigation_contexts_status_created",
                table: "ops_ts_investigation_contexts",
                columns: new[] { "Status", "CreatedAt" });

            // Outbox
            migrationBuilder.CreateIndex(
                name: "IX_ops_telstore_outbox_messages_CreatedAt",
                table: "ops_telstore_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_telstore_outbox_messages_ProcessedAt",
                table: "ops_telstore_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_telstore_outbox_messages_IdempotencyKey",
                table: "ops_telstore_outbox_messages",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ops_ts_service_metrics");
            migrationBuilder.DropTable(name: "ops_ts_dependency_metrics");
            migrationBuilder.DropTable(name: "ops_ts_observed_topology");
            migrationBuilder.DropTable(name: "ops_ts_anomaly_snapshots");
            migrationBuilder.DropTable(name: "ops_ts_telemetry_references");
            migrationBuilder.DropTable(name: "ops_ts_release_correlations");
            migrationBuilder.DropTable(name: "ops_ts_investigation_contexts");
            migrationBuilder.DropTable(name: "ops_telstore_outbox_messages");
        }
    }
}
