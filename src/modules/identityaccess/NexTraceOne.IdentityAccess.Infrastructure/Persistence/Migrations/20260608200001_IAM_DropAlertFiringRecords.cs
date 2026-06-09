using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IAM_DropAlertFiringRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fase 1.6: AlertFiringRecord movido para módulo OperationalIntelligence (opi_alert_firing_records).
            // Os dados históricos existentes na tabela iam_alert_firing_records são descartados.
            migrationBuilder.DropIndex(
                name: "ix_iam_alert_firing_rule",
                table: "iam_alert_firing_records");

            migrationBuilder.DropIndex(
                name: "ix_iam_alert_firing_tenant_fired",
                table: "iam_alert_firing_records");

            migrationBuilder.DropIndex(
                name: "ix_iam_alert_firing_tenant_status",
                table: "iam_alert_firing_records");

            migrationBuilder.DropTable(name: "iam_alert_firing_records");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "iam_alert_firing_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertRuleName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConditionSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NotificationChannels = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_alert_firing_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_iam_alert_firing_rule",
                table: "iam_alert_firing_records",
                column: "AlertRuleId");

            migrationBuilder.CreateIndex(
                name: "ix_iam_alert_firing_tenant_fired",
                table: "iam_alert_firing_records",
                columns: new[] { "TenantId", "FiredAt" });

            migrationBuilder.CreateIndex(
                name: "ix_iam_alert_firing_tenant_status",
                table: "iam_alert_firing_records",
                columns: new[] { "TenantId", "Status" });
        }
    }
}
