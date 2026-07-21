using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncIncidentsAndMapTypedIdFks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcknowledgedAt",
                table: "Incidents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcknowledgedBy",
                table: "Incidents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResolvedAt",
                table: "Incidents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RootCause",
                table: "Incidents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SlaBreached",
                table: "Incidents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowId",
                table: "AutomationValidations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowId",
                table: "AutomationAuditRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DriftFindingId",
                table: "AnomalyNarratives",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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
                name: "IX_AutomationValidations_WorkflowId",
                table: "AutomationValidations",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationAuditRecords_WorkflowId",
                table: "AutomationAuditRecords",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyNarratives_DriftFindingId",
                table: "AnomalyNarratives",
                column: "DriftFindingId");

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
            migrationBuilder.DropTable(
                name: "opi_alert_firing_records");

            migrationBuilder.DropIndex(
                name: "IX_AutomationValidations_WorkflowId",
                table: "AutomationValidations");

            migrationBuilder.DropIndex(
                name: "IX_AutomationAuditRecords_WorkflowId",
                table: "AutomationAuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AnomalyNarratives_DriftFindingId",
                table: "AnomalyNarratives");

            migrationBuilder.DropColumn(
                name: "AcknowledgedAt",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "AcknowledgedBy",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "RootCause",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "SlaBreached",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "AutomationValidations");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "AutomationAuditRecords");

            migrationBuilder.DropColumn(
                name: "DriftFindingId",
                table: "AnomalyNarratives");
        }
    }
}
