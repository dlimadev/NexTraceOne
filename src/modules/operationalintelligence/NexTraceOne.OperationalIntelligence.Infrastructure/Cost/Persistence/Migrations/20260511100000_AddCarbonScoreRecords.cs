using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCarbonScoreRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "oi_carbon_score_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CpuHours = table.Column<double>(type: "double precision", nullable: false),
                    MemoryGbHours = table.Column<double>(type: "double precision", nullable: false),
                    NetworkGb = table.Column<double>(type: "double precision", nullable: false),
                    CarbonGrams = table.Column<double>(type: "double precision", nullable: false),
                    IntensityFactor = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oi_carbon_score_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_oi_carbon_score_records_tenant_date",
                table: "oi_carbon_score_records",
                columns: new[] { "TenantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "ix_oi_carbon_score_records_tenant_service_date",
                table: "oi_carbon_score_records",
                columns: new[] { "TenantId", "ServiceId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "oi_carbon_score_records");
        }
    }
}
