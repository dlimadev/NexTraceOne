using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations;

/// <summary>
/// Wave V3.1 — Dashboard Intelligence Foundation.
/// Adiciona:
///   - Tabela gov_dashboard_revisions (histórico imutável de snapshots de dashboard)
///   - Colunas SharingPolicyJson, VariablesJson, CurrentRevisionNumber em gov_custom_dashboards
///   - Remove coluna IsShared (substituída por SharingPolicyJson)
/// Backward-compat: migra IsShared=true para SharingPolicy Tenant/Read via UPDATE.
/// </summary>
public partial class V31_DashboardIntelligenceFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── 1. Nova tabela de revisões de dashboard ──────────────────────────────────
        migrationBuilder.CreateTable(
            name: "gov_dashboard_revisions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DashboardId = table.Column<Guid>(type: "uuid", nullable: false),
                RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Layout = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                WidgetsJson = table.Column<string>(type: "jsonb", nullable: false),
                VariablesJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                AuthorUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ChangeNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gov_dashboard_revisions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_gov_dashboard_revisions_dashboard_tenant",
            table: "gov_dashboard_revisions",
            columns: new[] { "DashboardId", "tenant_id" });

        migrationBuilder.CreateIndex(
            name: "ix_gov_dashboard_revisions_dashboard_number",
            table: "gov_dashboard_revisions",
            columns: new[] { "DashboardId", "RevisionNumber" });

        migrationBuilder.CreateIndex(
            name: "ix_gov_dashboard_revisions_tenant",
            table: "gov_dashboard_revisions",
            column: "tenant_id");

        // ── 2. Adicionar colunas novas em gov_custom_dashboards ──────────────────────

        migrationBuilder.AddColumn<string>(
            name: "SharingPolicyJson",
            table: "gov_custom_dashboards",
            type: "jsonb",
            nullable: false,
            defaultValue: @"{""scope"":0,""permission"":0,""signedLinkExpiresAt"":null}");

        migrationBuilder.AddColumn<string>(
            name: "VariablesJson",
            table: "gov_custom_dashboards",
            type: "jsonb",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<int>(
            name: "CurrentRevisionNumber",
            table: "gov_custom_dashboards",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        // ── 3. Migrar IsShared → SharingPolicyJson (backward-compat) ────────────────
        migrationBuilder.Sql(@"
            UPDATE gov_custom_dashboards
            SET ""SharingPolicyJson"" = '{ ""scope"": 2, ""permission"": 0, ""signedLinkExpiresAt"": null }'
            WHERE ""IsShared"" = true;
        ");

        // ── 4. Remover coluna legada IsShared ────────────────────────────────────────
        migrationBuilder.DropColumn(
            name: "IsShared",
            table: "gov_custom_dashboards");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Restaura IsShared a partir de SharingPolicyJson
        migrationBuilder.AddColumn<bool>(
            name: "IsShared",
            table: "gov_custom_dashboards",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql(@"
            UPDATE gov_custom_dashboards
            SET ""IsShared"" = true
            WHERE (""SharingPolicyJson""::jsonb)->>'scope' NOT IN ('0');
        ");

        migrationBuilder.DropColumn(name: "CurrentRevisionNumber", table: "gov_custom_dashboards");
        migrationBuilder.DropColumn(name: "VariablesJson", table: "gov_custom_dashboards");
        migrationBuilder.DropColumn(name: "SharingPolicyJson", table: "gov_custom_dashboards");

        migrationBuilder.DropTable(name: "gov_dashboard_revisions");
    }
}
