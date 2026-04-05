using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P44_BudgetForecastAndEfficiencyRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ops_cost_budget_forecasts");

            migrationBuilder.DropTable(
                name: "ops_cost_efficiency_recommendations");
        }
    }
}
