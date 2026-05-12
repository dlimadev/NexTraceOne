using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "oi_waste_signals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    signal_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    estimated_monthly_savings = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    team_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_acknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acknowledged_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    detected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_waste_signals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_attributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    RequestCount = table.Column<long>(type: "bigint", nullable: false),
                    CostPerRequest = table.Column<decimal>(type: "numeric", nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_attributions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_budget_forecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ForecastPeriod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProjectedCost = table.Column<decimal>(type: "numeric", nullable: false),
                    BudgetLimit = table.Column<decimal>(type: "numeric", nullable: true),
                    ConfidencePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    IsOverBudgetProjected = table.Column<bool>(type: "boolean", nullable: false),
                    ForecastNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_budget_forecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_efficiency_recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceCost = table.Column<decimal>(type: "numeric", nullable: false),
                    MedianPeerCost = table.Column<decimal>(type: "numeric", nullable: false),
                    DeviationPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    RecommendationText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_efficiency_recommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_import_batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RecordCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_import_batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Team = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Domain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    CpuCostShare = table.Column<decimal>(type: "numeric", nullable: false),
                    MemoryCostShare = table.Column<decimal>(type: "numeric", nullable: false),
                    NetworkCostShare = table.Column<decimal>(type: "numeric", nullable: false),
                    StorageCostShare = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_cost_trends",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AverageDailyCost = table.Column<decimal>(type: "numeric", nullable: false),
                    PeakDailyCost = table.Column<decimal>(type: "numeric", nullable: false),
                    TrendDirection = table.Column<int>(type: "integer", nullable: false),
                    PercentageChange = table.Column<decimal>(type: "numeric", nullable: false),
                    DataPointCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_cost_trends", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_outbox_messages",
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
                    table.PrimaryKey("PK_ops_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ops_service_cost_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MonthlyBudget = table.Column<decimal>(type: "numeric", nullable: true),
                    CurrentMonthCost = table.Column<decimal>(type: "numeric", nullable: false),
                    AlertThresholdPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_service_cost_profiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_oi_waste_signals_service",
                table: "oi_waste_signals",
                column: "service_name");

            migrationBuilder.CreateIndex(
                name: "ix_oi_waste_signals_team_ack",
                table: "oi_waste_signals",
                columns: new[] { "team_name", "is_acknowledged" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_attributions_ApiAssetId_Environment_PeriodStart_Pe~",
                table: "ops_cost_attributions",
                columns: new[] { "ApiAssetId", "Environment", "PeriodStart", "PeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_attributions_ServiceName",
                table: "ops_cost_attributions",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_budget_forecasts_ComputedAt",
                table: "ops_cost_budget_forecasts",
                column: "ComputedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_budget_forecasts_ServiceId_Environment",
                table: "ops_cost_budget_forecasts",
                columns: new[] { "ServiceId", "Environment" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_efficiency_recommendations_IsAcknowledged",
                table: "ops_cost_efficiency_recommendations",
                column: "IsAcknowledged");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_efficiency_recommendations_ServiceId",
                table: "ops_cost_efficiency_recommendations",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_import_batches_Source_Period",
                table: "ops_cost_import_batches",
                columns: new[] { "Source", "Period" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_import_batches_Status",
                table: "ops_cost_import_batches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_BatchId",
                table: "ops_cost_records",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_Domain",
                table: "ops_cost_records",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_Period",
                table: "ops_cost_records",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_ReleaseId",
                table: "ops_cost_records",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_ServiceId_Period",
                table: "ops_cost_records",
                columns: new[] { "ServiceId", "Period" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_records_Team",
                table: "ops_cost_records",
                column: "Team");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_snapshots_Period",
                table: "ops_cost_snapshots",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_snapshots_ServiceName_Environment_CapturedAt",
                table: "ops_cost_snapshots",
                columns: new[] { "ServiceName", "Environment", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_trends_ServiceName_Environment_PeriodStart_PeriodE~",
                table: "ops_cost_trends",
                columns: new[] { "ServiceName", "Environment", "PeriodStart", "PeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_cost_trends_TrendDirection",
                table: "ops_cost_trends",
                column: "TrendDirection");

            migrationBuilder.CreateIndex(
                name: "IX_ops_outbox_messages_CreatedAt",
                table: "ops_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_outbox_messages_IdempotencyKey",
                table: "ops_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ops_outbox_messages_ProcessedAt",
                table: "ops_outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ops_service_cost_profiles_ServiceName_Environment",
                table: "ops_service_cost_profiles",
                columns: new[] { "ServiceName", "Environment" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "oi_waste_signals");

            migrationBuilder.DropTable(
                name: "ops_cost_attributions");

            migrationBuilder.DropTable(
                name: "ops_cost_budget_forecasts");

            migrationBuilder.DropTable(
                name: "ops_cost_efficiency_recommendations");

            migrationBuilder.DropTable(
                name: "ops_cost_import_batches");

            migrationBuilder.DropTable(
                name: "ops_cost_records");

            migrationBuilder.DropTable(
                name: "ops_cost_snapshots");

            migrationBuilder.DropTable(
                name: "ops_cost_trends");

            migrationBuilder.DropTable(
                name: "ops_outbox_messages");

            migrationBuilder.DropTable(
                name: "ops_service_cost_profiles");
        }
    }
}
