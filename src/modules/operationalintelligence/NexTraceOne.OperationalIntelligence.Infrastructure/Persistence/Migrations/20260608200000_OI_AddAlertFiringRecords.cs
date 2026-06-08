using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OI_AddAlertFiringRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fase 1.6: AlertFiringRecord migrado de IAM (iam_alert_firing_records) para OI (opi_alert_firing_records)
            migrationBuilder.CreateTable(
                name: "opi_alert_firing_records",
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
                    table.PrimaryKey("PK_opi_alert_firing_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_opi_alert_firing_rule",
                table: "opi_alert_firing_records",
                column: "AlertRuleId");

            migrationBuilder.CreateIndex(
                name: "ix_opi_alert_firing_tenant_fired",
                table: "opi_alert_firing_records",
                columns: new[] { "TenantId", "FiredAt" });

            migrationBuilder.CreateIndex(
                name: "ix_opi_alert_firing_tenant_status",
                table: "opi_alert_firing_records",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "opi_alert_firing_records");
        }
    }
}
