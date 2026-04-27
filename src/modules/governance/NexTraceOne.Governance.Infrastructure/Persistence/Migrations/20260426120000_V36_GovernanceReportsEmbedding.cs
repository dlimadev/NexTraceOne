using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations;

/// <summary>
/// Wave V3.6 — Governance, Reports &amp; Embedding.
/// Adiciona:
///   - Tabela gov_scheduled_dashboard_reports (agendamentos PDF/PNG com cron + SMTP/webhook)
///   - Tabela gov_dashboard_usage_events (analytics de uso por dashboard)
///   - Colunas de ciclo de vida em gov_custom_dashboards (LifecycleStatus, DeprecatedAt, DeprecatedByUserId,
///     DeprecationNote, SuccessorDashboardId)
/// </summary>
public partial class V36_GovernanceReportsEmbedding : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── 1. Agendamentos de relatórios ────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "gov_scheduled_dashboard_reports",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DashboardId = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CronExpression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "pdf"),
                RecipientsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                WebhookUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                RetentionDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 90),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                LastRunAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                NextRunAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                SuccessCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                FailureCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                LastFailureMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gov_scheduled_dashboard_reports", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_gov_sched_report_dashboard_tenant",
            table: "gov_scheduled_dashboard_reports",
            columns: new[] { "DashboardId", "tenant_id" });

        migrationBuilder.CreateIndex(
            name: "ix_gov_sched_report_tenant_active",
            table: "gov_scheduled_dashboard_reports",
            columns: new[] { "tenant_id", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "ix_gov_sched_report_next_run",
            table: "gov_scheduled_dashboard_reports",
            column: "NextRunAt");

        // ── 2. Eventos de uso de dashboards ─────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "gov_dashboard_usage_events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DashboardId = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Persona = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                EventType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "view"),
                DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gov_dashboard_usage_events", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_gov_dash_usage_dashboard_tenant",
            table: "gov_dashboard_usage_events",
            columns: new[] { "DashboardId", "tenant_id" });

        migrationBuilder.CreateIndex(
            name: "ix_gov_dash_usage_occurred_at",
            table: "gov_dashboard_usage_events",
            column: "OccurredAt");

        migrationBuilder.CreateIndex(
            name: "ix_gov_dash_usage_tenant_type",
            table: "gov_dashboard_usage_events",
            columns: new[] { "tenant_id", "EventType" });

        // ── 3. Colunas de ciclo de vida em gov_custom_dashboards ─────────────────────
        migrationBuilder.AddColumn<int>(
            name: "LifecycleStatus",
            table: "gov_custom_dashboards",
            type: "integer",
            nullable: false,
            defaultValue: 1); // Published (backward-compat: existing dashboards são Published)

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "DeprecatedAt",
            table: "gov_custom_dashboards",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DeprecatedByUserId",
            table: "gov_custom_dashboards",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DeprecationNote",
            table: "gov_custom_dashboards",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "SuccessorDashboardId",
            table: "gov_custom_dashboards",
            type: "uuid",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "gov_scheduled_dashboard_reports");
        migrationBuilder.DropTable(name: "gov_dashboard_usage_events");

        migrationBuilder.DropColumn(name: "LifecycleStatus", table: "gov_custom_dashboards");
        migrationBuilder.DropColumn(name: "DeprecatedAt", table: "gov_custom_dashboards");
        migrationBuilder.DropColumn(name: "DeprecatedByUserId", table: "gov_custom_dashboards");
        migrationBuilder.DropColumn(name: "DeprecationNote", table: "gov_custom_dashboards");
        migrationBuilder.DropColumn(name: "SuccessorDashboardId", table: "gov_custom_dashboards");
    }
}
