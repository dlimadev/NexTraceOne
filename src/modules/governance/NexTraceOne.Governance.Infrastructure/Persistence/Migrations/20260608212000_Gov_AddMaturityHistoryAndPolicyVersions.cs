using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Gov_AddMaturityHistoryAndPolicyVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fase 4.3: Histórico de snapshots de maturidade por reavaliação
            migrationBuilder.CreateTable(
                name: "gov_service_maturity_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReassessmentCount = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_service_maturity_history", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gov_service_maturity_history_AssessmentId",
                table: "gov_service_maturity_history",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_service_maturity_history_ServiceId",
                table: "gov_service_maturity_history",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_service_maturity_history_RecordedAt",
                table: "gov_service_maturity_history",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_gov_service_maturity_history_tenant_id",
                table: "gov_service_maturity_history",
                column: "tenant_id");

            // Fase 4.4: Snapshots imutáveis de versões de definições de política como código
            migrationBuilder.CreateTable(
                name: "gov_policy_definition_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DefinitionContent = table.Column<string>(type: "text", nullable: false),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChangeNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gov_policy_definition_versions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gov_policy_definition_versions_PolicyId",
                table: "gov_policy_definition_versions",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_policy_definition_versions_TenantId",
                table: "gov_policy_definition_versions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_gov_policy_definition_versions_PolicyId_Version",
                table: "gov_policy_definition_versions",
                columns: new[] { "PolicyId", "Version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "gov_service_maturity_history");
            migrationBuilder.DropTable(name: "gov_policy_definition_versions");
        }
    }
}
