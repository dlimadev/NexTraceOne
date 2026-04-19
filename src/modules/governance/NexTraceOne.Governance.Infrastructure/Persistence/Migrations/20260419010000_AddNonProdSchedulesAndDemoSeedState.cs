using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adiciona tabelas de persistência para agendas de ambientes não produtivos, estado do seed de demonstração
/// e configuração SAML SSO, substituindo dados em memória por dados persistidos em base de dados.
/// </summary>
public partial class AddNonProdSchedulesAndDemoSeedState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // gov_nonprod_schedules
        migrationBuilder.CreateTable(
            name: "gov_nonprod_schedules",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EnvironmentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                EnvironmentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Enabled = table.Column<bool>(type: "boolean", nullable: false),
                ActiveDaysOfWeekJson = table.Column<string>(type: "jsonb", nullable: false),
                ActiveFromHour = table.Column<int>(type: "integer", nullable: false),
                ActiveToHour = table.Column<int>(type: "integer", nullable: false),
                Timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                EstimatedSavingPct = table.Column<int>(type: "integer", nullable: false),
                KeepActiveUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                OverrideReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gov_nonprod_schedules", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_gov_nonprod_schedules_EnvironmentId_TenantId",
            table: "gov_nonprod_schedules",
            columns: new[] { "EnvironmentId", "TenantId" },
            unique: true);

        // gov_demo_seed_state
        migrationBuilder.CreateTable(
            name: "gov_demo_seed_state",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                SeededAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                EntitiesCount = table.Column<int>(type: "integer", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gov_demo_seed_state", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_gov_demo_seed_state_TenantId",
            table: "gov_demo_seed_state",
            column: "TenantId");

        // gov_saml_sso_configurations
        migrationBuilder.CreateTable(
            name: "gov_saml_sso_configurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EntityId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                SsoUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                SloUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                IdpCertificate = table.Column<string>(type: "text", nullable: true),
                JitProvisioningEnabled = table.Column<bool>(type: "boolean", nullable: false),
                DefaultRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                AttributeMappingsJson = table.Column<string>(type: "jsonb", nullable: true),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gov_saml_sso_configurations", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_gov_saml_sso_configurations_TenantId",
            table: "gov_saml_sso_configurations",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "gov_nonprod_schedules");
        migrationBuilder.DropTable(name: "gov_demo_seed_state");
        migrationBuilder.DropTable(name: "gov_saml_sso_configurations");
    }
}
