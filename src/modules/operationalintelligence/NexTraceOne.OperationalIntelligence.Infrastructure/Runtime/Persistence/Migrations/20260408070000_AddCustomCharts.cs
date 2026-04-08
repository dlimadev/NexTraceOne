using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomCharts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ops_custom_charts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ChartType = table.Column<int>(type: "integer", nullable: false),
                    MetricQuery = table.Column<string>(type: "text", nullable: false),
                    TimeRange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FiltersJson = table.Column<string>(type: "text", nullable: true),
                    IsShared = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ops_custom_charts", x => x.Id);
                    table.CheckConstraint("CK_ops_custom_charts_chart_type", "\"ChartType\" >= 0 AND \"ChartType\" <= 6");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ops_custom_charts_IsShared",
                table: "ops_custom_charts",
                column: "IsShared");

            migrationBuilder.CreateIndex(
                name: "IX_ops_custom_charts_TenantId",
                table: "ops_custom_charts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ops_custom_charts_UserId_TenantId",
                table: "ops_custom_charts",
                columns: new[] { "UserId", "TenantId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ops_custom_charts");
        }
    }
}
