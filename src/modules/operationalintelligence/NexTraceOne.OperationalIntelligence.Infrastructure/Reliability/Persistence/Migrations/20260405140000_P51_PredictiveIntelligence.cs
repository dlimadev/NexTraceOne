using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P51_PredictiveIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ops_reliability_failure_predictions");
            migrationBuilder.DropTable(name: "ops_reliability_capacity_forecasts");
        }
    }
}
