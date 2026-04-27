using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations;

/// <summary>
/// Wave V3.9 — Advanced NQL, Alerting from Widget &amp; Mobile On-Call Companion.
/// Adiciona:
///   - Tabela gov_dashboard_monitors (monitores de alerta criados a partir de QueryWidgets)
/// </summary>
public partial class V39_NqlAlertingMobile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "gov_dashboard_monitors",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DashboardId = table.Column<Guid>(type: "uuid", nullable: false),
                WidgetId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                NqlQuery = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                ConditionField = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ConditionOperator = table.Column<int>(type: "integer", nullable: false),
                ConditionThreshold = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                EvaluationWindowMinutes = table.Column<int>(type: "integer", nullable: false),
                Severity = table.Column<int>(type: "integer", nullable: false),
                NotificationChannelsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                LastFiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                FiredCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gov_dashboard_monitors", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_gov_monitor_dashboard_tenant",
            table: "gov_dashboard_monitors",
            columns: new[] { "DashboardId", "tenant_id" });

        migrationBuilder.CreateIndex(
            name: "ix_gov_monitor_tenant_status",
            table: "gov_dashboard_monitors",
            columns: new[] { "tenant_id", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "gov_dashboard_monitors");
    }
}
