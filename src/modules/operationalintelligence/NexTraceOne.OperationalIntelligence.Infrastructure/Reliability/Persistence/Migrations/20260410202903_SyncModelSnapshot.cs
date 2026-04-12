using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ops_reliability_healing_recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RootCauseDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionDetails = table.Column<string>(type: "jsonb", nullable: false),
                    ConfidenceScore = table.Column<int>(type: "integer", nullable: false),
                    EstimatedImpact = table.Column<string>(type: "jsonb", nullable: true),
                    RelatedRunbookIds = table.Column<string>(type: "jsonb", nullable: true),
                    HistoricalSuccessRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ApprovedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExecutionStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExecutionCompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExecutionResult = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    EvidenceTrail = table.Column<string>(type: "jsonb", nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_reliability_healing_recommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_reliability_incident_prediction_patterns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatternName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PatternType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConfidencePercent = table.Column<int>(type: "integer", nullable: false),
                    OccurrenceCount = table.Column<int>(type: "integer", nullable: false),
                    SampleSize = table.Column<int>(type: "integer", nullable: false),
                    Evidence = table.Column<string>(type: "jsonb", nullable: false),
                    TriggerConditions = table.Column<string>(type: "jsonb", nullable: false),
                    PreventionRecommendations = table.Column<string>(type: "jsonb", nullable: true),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidationComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_reliability_incident_prediction_patterns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ops_reliability_healing_recommendations_ServiceName",
                table: "ops_reliability_healing_recommendations",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_ops_reliability_healing_recommendations_ServiceName_Status_~",
                table: "ops_reliability_healing_recommendations",
                columns: new[] { "ServiceName", "Status", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_reliability_healing_recommendations_Status",
                table: "ops_reliability_healing_recommendations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_ops_reliability_healing_recommendations_tenant_id",
                table: "ops_reliability_healing_recommendations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_reliability_incident_prediction_patterns_Environment",
                table: "ops_reliability_incident_prediction_patterns",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_ops_reliability_incident_prediction_patterns_PatternType",
                table: "ops_reliability_incident_prediction_patterns",
                column: "PatternType");

            migrationBuilder.CreateIndex(
                name: "IX_ops_reliability_incident_prediction_patterns_ServiceId_Envi~",
                table: "ops_reliability_incident_prediction_patterns",
                columns: new[] { "ServiceId", "Environment", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_reliability_incident_prediction_patterns_Status",
                table: "ops_reliability_incident_prediction_patterns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_ops_reliability_incident_prediction_patterns_tenant_id",
                table: "ops_reliability_incident_prediction_patterns",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ops_reliability_healing_recommendations");

            migrationBuilder.DropTable(
                name: "ops_reliability_incident_prediction_patterns");
        }
    }
}
