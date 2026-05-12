using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ops_rel_outbox_messages",
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
                    table.PrimaryKey("PK_ops_rel_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_reliability_capacity_forecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CurrentUtilizationPercent = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    GrowthRatePercentPerDay = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    EstimatedDaysToSaturation = table.Column<int>(type: "integer", nullable: true),
                    SaturationRisk = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_reliability_capacity_forecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_reliability_failure_predictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FailureProbabilityPercent = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PredictionHorizon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CausalFactors = table.Column<string>(type: "jsonb", nullable: false),
                    RecommendedAction = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_reliability_failure_predictions", x => x.Id);
                });

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
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "ops_reliability_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric", nullable: false),
                    RuntimeHealthScore = table.Column<decimal>(type: "numeric", nullable: false),
                    IncidentImpactScore = table.Column<decimal>(type: "numeric", nullable: false),
                    ObservabilityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    OpenIncidentCount = table.Column<int>(type: "integer", nullable: false),
                    RuntimeHealthStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TrendDirection = table.Column<int>(type: "integer", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_reliability_snapshots", x => x.Id);
                    table.CheckConstraint("CK_ops_reliability_snapshots_trend", "\"TrendDirection\" >= 0 AND \"TrendDirection\" <= 2");
                });

            migrationBuilder.CreateTable(
                name: "ops_slo_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TargetPercent = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    AlertThresholdPercent = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    WindowDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_slo_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_burn_rate_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SloDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Window = table.Column<int>(type: "integer", nullable: false),
                    BurnRate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ObservedErrorRate = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    ToleratedErrorRate = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_burn_rate_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ops_burn_rate_snapshots_ops_slo_definitions_SloDefinitionId",
                        column: x => x.SloDefinitionId,
                        principalTable: "ops_slo_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ops_error_budget_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SloDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TotalBudgetMinutes = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ConsumedBudgetMinutes = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    RemainingBudgetMinutes = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ConsumedPercent = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_error_budget_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ops_error_budget_snapshots_ops_slo_definitions_SloDefinitio~",
                        column: x => x.SloDefinitionId,
                        principalTable: "ops_slo_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ops_sla_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SloDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ContractualTargetPercent = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HasPenaltyClauses = table.Column<bool>(type: "boolean", nullable: false),
                    PenaltyNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_sla_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ops_sla_definitions_ops_slo_definitions_SloDefinitionId",
                        column: x => x.SloDefinitionId,
                        principalTable: "ops_slo_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ops_burn_rate_snapshots_SloDefinitionId",
                table: "ops_burn_rate_snapshots",
                column: "SloDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_burn_rate_snapshots_TenantId_ServiceId_Window_ComputedAt",
                table: "ops_burn_rate_snapshots",
                columns: new[] { "TenantId", "ServiceId", "Window", "ComputedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_burn_rate_snapshots_TenantId_SloDefinitionId_ComputedAt",
                table: "ops_burn_rate_snapshots",
                columns: new[] { "TenantId", "SloDefinitionId", "ComputedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_error_budget_snapshots_SloDefinitionId",
                table: "ops_error_budget_snapshots",
                column: "SloDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_error_budget_snapshots_TenantId_ServiceId_ComputedAt",
                table: "ops_error_budget_snapshots",
                columns: new[] { "TenantId", "ServiceId", "ComputedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_error_budget_snapshots_TenantId_SloDefinitionId_Compute~",
                table: "ops_error_budget_snapshots",
                columns: new[] { "TenantId", "SloDefinitionId", "ComputedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_rel_outbox_messages_CreatedAt",
                table: "ops_rel_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_rel_outbox_messages_IdempotencyKey",
                table: "ops_rel_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_rel_outbox_messages_ProcessedAt",
                table: "ops_rel_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_reliability_capacity_forecasts_Environment",
                table: "ops_reliability_capacity_forecasts",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_ops_reliability_capacity_forecasts_SaturationRisk",
                table: "ops_reliability_capacity_forecasts",
                column: "SaturationRisk");

            migrationBuilder.CreateIndex(
                name: "IX_ops_reliability_capacity_forecasts_ServiceId",
                table: "ops_reliability_capacity_forecasts",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_reliability_failure_predictions_Environment",
                table: "ops_reliability_failure_predictions",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_ops_reliability_failure_predictions_RiskLevel",
                table: "ops_reliability_failure_predictions",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_ops_reliability_failure_predictions_ServiceId",
                table: "ops_reliability_failure_predictions",
                column: "ServiceId");

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

            migrationBuilder.CreateIndex(
                name: "IX_ops_reliability_snapshots_TenantId_ComputedAt",
                table: "ops_reliability_snapshots",
                columns: new[] { "TenantId", "ComputedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_reliability_snapshots_TenantId_ServiceId_ComputedAt",
                table: "ops_reliability_snapshots",
                columns: new[] { "TenantId", "ServiceId", "ComputedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_sla_definitions_SloDefinitionId",
                table: "ops_sla_definitions",
                column: "SloDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_sla_definitions_TenantId_IsActive",
                table: "ops_sla_definitions",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_sla_definitions_TenantId_SloDefinitionId",
                table: "ops_sla_definitions",
                columns: new[] { "TenantId", "SloDefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_slo_definitions_TenantId_IsActive",
                table: "ops_slo_definitions",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_slo_definitions_TenantId_ServiceId_Environment",
                table: "ops_slo_definitions",
                columns: new[] { "TenantId", "ServiceId", "Environment" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ops_burn_rate_snapshots");

            migrationBuilder.DropTable(
                name: "ops_error_budget_snapshots");

            migrationBuilder.DropTable(
                name: "ops_rel_outbox_messages");

            migrationBuilder.DropTable(
                name: "ops_reliability_capacity_forecasts");

            migrationBuilder.DropTable(
                name: "ops_reliability_failure_predictions");

            migrationBuilder.DropTable(
                name: "ops_reliability_healing_recommendations");

            migrationBuilder.DropTable(
                name: "ops_reliability_incident_prediction_patterns");

            migrationBuilder.DropTable(
                name: "ops_reliability_snapshots");

            migrationBuilder.DropTable(
                name: "ops_sla_definitions");

            migrationBuilder.DropTable(
                name: "ops_slo_definitions");
        }
    }
}
