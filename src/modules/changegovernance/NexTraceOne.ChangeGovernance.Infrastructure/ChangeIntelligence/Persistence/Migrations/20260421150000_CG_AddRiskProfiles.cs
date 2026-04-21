using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CG_AddRiskProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tabela de perfis de risco de serviços — Risk Center (Wave F.2)
            migrationBuilder.CreateTable(
                name: "chg_service_risk_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    OverallRiskLevel = table.Column<int>(type: "integer", nullable: false),
                    OverallScore = table.Column<int>(type: "integer", nullable: false),
                    VulnerabilityScore = table.Column<int>(type: "integer", nullable: false),
                    ChangeFailureScore = table.Column<int>(type: "integer", nullable: false),
                    BlastRadiusScore = table.Column<int>(type: "integer", nullable: false),
                    PolicyViolationScore = table.Column<int>(type: "integer", nullable: false),
                    ActiveSignalsJson = table.Column<string>(type: "text", nullable: false),
                    ActiveSignalCount = table.Column<int>(type: "integer", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chg_service_risk_profiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_chg_risk_profiles_tenant_service_computed",
                table: "chg_service_risk_profiles",
                columns: new[] { "TenantId", "ServiceAssetId", "ComputedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_chg_risk_profiles_tenant_risk_level",
                table: "chg_service_risk_profiles",
                columns: new[] { "TenantId", "OverallRiskLevel" });

            migrationBuilder.CreateIndex(
                name: "ix_chg_risk_profiles_tenant_score",
                table: "chg_service_risk_profiles",
                columns: new[] { "TenantId", "OverallScore" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "chg_service_risk_profiles");
        }
    }
}
