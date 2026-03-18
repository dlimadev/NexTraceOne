using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialRuntimeIntelligenceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "oi_drift_findings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MetricName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExpectedValue = table.Column<decimal>(type: "numeric", nullable: false),
                    ActualValue = table.Column<decimal>(type: "numeric", nullable: false),
                    DeviationPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolutionComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_drift_findings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oi_observability_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    HasTracing = table.Column<bool>(type: "boolean", nullable: false),
                    HasMetrics = table.Column<bool>(type: "boolean", nullable: false),
                    HasLogging = table.Column<bool>(type: "boolean", nullable: false),
                    HasAlerting = table.Column<bool>(type: "boolean", nullable: false),
                    HasDashboard = table.Column<bool>(type: "boolean", nullable: false),
                    ObservabilityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    LastAssessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_observability_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oi_runtime_baselines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExpectedAvgLatencyMs = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpectedP99LatencyMs = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpectedErrorRate = table.Column<decimal>(type: "numeric", nullable: false),
                    ExpectedRequestsPerSecond = table.Column<decimal>(type: "numeric", nullable: false),
                    EstablishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DataPointCount = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_runtime_baselines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oi_runtime_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AvgLatencyMs = table.Column<decimal>(type: "numeric", nullable: false),
                    P99LatencyMs = table.Column<decimal>(type: "numeric", nullable: false),
                    ErrorRate = table.Column<decimal>(type: "numeric", nullable: false),
                    RequestsPerSecond = table.Column<decimal>(type: "numeric", nullable: false),
                    CpuUsagePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    MemoryUsageMb = table.Column<decimal>(type: "numeric", nullable: false),
                    ActiveInstances = table.Column<int>(type: "integer", nullable: false),
                    HealthStatus = table.Column<int>(type: "integer", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_runtime_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_oi_drift_findings_IsResolved",
                table: "oi_drift_findings",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_oi_drift_findings_ServiceName_Environment_DetectedAt",
                table: "oi_drift_findings",
                columns: new[] { "ServiceName", "Environment", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_oi_drift_findings_Severity",
                table: "oi_drift_findings",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_oi_observability_profiles_LastAssessedAt",
                table: "oi_observability_profiles",
                column: "LastAssessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_oi_observability_profiles_ServiceName_Environment",
                table: "oi_observability_profiles",
                columns: new[] { "ServiceName", "Environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oi_runtime_baselines_EstablishedAt",
                table: "oi_runtime_baselines",
                column: "EstablishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_oi_runtime_baselines_ServiceName_Environment",
                table: "oi_runtime_baselines",
                columns: new[] { "ServiceName", "Environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oi_runtime_snapshots_HealthStatus",
                table: "oi_runtime_snapshots",
                column: "HealthStatus");

            migrationBuilder.CreateIndex(
                name: "IX_oi_runtime_snapshots_ServiceName_Environment_CapturedAt",
                table: "oi_runtime_snapshots",
                columns: new[] { "ServiceName", "Environment", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_CreatedAt",
                table: "outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt",
                table: "outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "oi_drift_findings");

            migrationBuilder.DropTable(
                name: "oi_observability_profiles");

            migrationBuilder.DropTable(
                name: "oi_runtime_baselines");

            migrationBuilder.DropTable(
                name: "oi_runtime_snapshots");

            migrationBuilder.DropTable(
                name: "outbox_messages");
        }
    }
}
