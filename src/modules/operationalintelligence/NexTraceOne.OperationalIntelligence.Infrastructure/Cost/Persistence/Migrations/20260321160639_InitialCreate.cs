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
                name: "oi_cost_attributions",
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
                    table.PrimaryKey("PK_oi_cost_attributions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oi_cost_snapshots",
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
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oi_cost_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oi_cost_trends",
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
                    table.PrimaryKey("PK_oi_cost_trends", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oi_service_cost_profiles",
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
                    table.PrimaryKey("PK_oi_service_cost_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "oi_cost_outbox_messages",
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
                    table.PrimaryKey("PK_oi_cost_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_oi_cost_attributions_ApiAssetId_Environment_PeriodStart_Per~",
                table: "oi_cost_attributions",
                columns: new[] { "ApiAssetId", "Environment", "PeriodStart", "PeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oi_cost_attributions_ServiceName",
                table: "oi_cost_attributions",
                column: "ServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_oi_cost_snapshots_Period",
                table: "oi_cost_snapshots",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_oi_cost_snapshots_ServiceName_Environment_CapturedAt",
                table: "oi_cost_snapshots",
                columns: new[] { "ServiceName", "Environment", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_oi_cost_trends_ServiceName_Environment_PeriodStart_PeriodEnd",
                table: "oi_cost_trends",
                columns: new[] { "ServiceName", "Environment", "PeriodStart", "PeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oi_cost_trends_TrendDirection",
                table: "oi_cost_trends",
                column: "TrendDirection");

            migrationBuilder.CreateIndex(
                name: "IX_oi_service_cost_profiles_ServiceName_Environment",
                table: "oi_service_cost_profiles",
                columns: new[] { "ServiceName", "Environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oi_cost_outbox_messages_CreatedAt",
                table: "oi_cost_outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_oi_cost_outbox_messages_IdempotencyKey",
                table: "oi_cost_outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_oi_cost_outbox_messages_ProcessedAt",
                table: "oi_cost_outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "oi_cost_attributions");

            migrationBuilder.DropTable(
                name: "oi_cost_snapshots");

            migrationBuilder.DropTable(
                name: "oi_cost_trends");

            migrationBuilder.DropTable(
                name: "oi_service_cost_profiles");

            migrationBuilder.DropTable(
                name: "oi_cost_outbox_messages");
        }
    }
}
