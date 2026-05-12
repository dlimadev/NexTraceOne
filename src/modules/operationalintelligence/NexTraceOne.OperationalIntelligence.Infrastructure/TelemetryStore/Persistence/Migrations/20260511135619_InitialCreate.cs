using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_ops_telstore_outbox_messages_CreatedAt",
                table: "ops_telstore_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_telstore_outbox_messages_IdempotencyKey",
                table: "ops_telstore_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_telstore_outbox_messages_ProcessedAt",
                table: "ops_telstore_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_anomaly_snapshots_ServiceId_Environment_DetectedAt",
                table: "ops_ts_anomaly_snapshots",
                columns: new[] { "ServiceId", "Environment", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_anomaly_snapshots_Severity_ResolvedAt",
                table: "ops_ts_anomaly_snapshots",
                columns: new[] { "Severity", "ResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_dependency_metrics_SourceServiceId_TargetServiceId_E~",
                table: "ops_ts_dependency_metrics",
                columns: new[] { "SourceServiceId", "TargetServiceId", "Environment", "AggregationLevel", "IntervalStart" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_investigation_contexts_PrimaryServiceId_Environment_~",
                table: "ops_ts_investigation_contexts",
                columns: new[] { "PrimaryServiceId", "Environment", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_investigation_contexts_Status_CreatedAt",
                table: "ops_ts_investigation_contexts",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_observed_topology_Environment_IsShadowDependency",
                table: "ops_ts_observed_topology",
                columns: new[] { "Environment", "IsShadowDependency" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_observed_topology_SourceServiceId_TargetServiceId_En~",
                table: "ops_ts_observed_topology",
                columns: new[] { "SourceServiceId", "TargetServiceId", "Environment", "CommunicationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_release_correlations_ReleaseId",
                table: "ops_ts_release_correlations",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_release_correlations_ServiceId_Environment_DeployedAt",
                table: "ops_ts_release_correlations",
                columns: new[] { "ServiceId", "Environment", "DeployedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_service_metrics_ServiceId_Environment_AggregationLev~",
                table: "ops_ts_service_metrics",
                columns: new[] { "ServiceId", "Environment", "AggregationLevel", "IntervalStart" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_service_metrics_ServiceName_Environment_IntervalStart",
                table: "ops_ts_service_metrics",
                columns: new[] { "ServiceName", "Environment", "IntervalStart" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_telemetry_references_CorrelationId",
                table: "ops_ts_telemetry_references",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_ts_telemetry_references_ServiceId_SignalType_OriginalTi~",
                table: "ops_ts_telemetry_references",
                columns: new[] { "ServiceId", "SignalType", "OriginalTimestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ops_telstore_outbox_messages");

            migrationBuilder.DropTable(
                name: "ops_ts_anomaly_snapshots");

            migrationBuilder.DropTable(
                name: "ops_ts_dependency_metrics");

            migrationBuilder.DropTable(
                name: "ops_ts_investigation_contexts");

            migrationBuilder.DropTable(
                name: "ops_ts_observed_topology");

            migrationBuilder.DropTable(
                name: "ops_ts_release_correlations");

            migrationBuilder.DropTable(
                name: "ops_ts_service_metrics");

            migrationBuilder.DropTable(
                name: "ops_ts_telemetry_references");
        }
    }
}
