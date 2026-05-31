using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ctr_code_quality_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    ProjectKey = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    QualityGateStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Coverage = table.Column<double>(type: "double precision", nullable: false),
                    Bugs = table.Column<int>(type: "integer", nullable: false),
                    Vulnerabilities = table.Column<int>(type: "integer", nullable: false),
                    CodeSmells = table.Column<int>(type: "integer", nullable: false),
                    DuplicatedLinesDensity = table.Column<double>(type: "double precision", nullable: false),
                    Branch = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AnalyzedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_code_quality_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ctr_code_quality_records_AnalyzedAt",
                table: "ctr_code_quality_records",
                column: "AnalyzedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_code_quality_records_TenantId",
                table: "ctr_code_quality_records",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_code_quality_records_TenantId_ServiceId",
                table: "ctr_code_quality_records",
                columns: new[] { "TenantId", "ServiceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ctr_code_quality_records");
        }
    }
}
