using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SaaS_AddLicensingAgentHeartbeatAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── TenantLicense ──────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "iam_tenant_licenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Plan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IncludedHostUnits = table.Column<int>(type: "integer", nullable: false),
                    CurrentHostUnits = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    BillingCycleStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_tenant_licenses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "uix_iam_tenant_licenses_tenant",
                table: "iam_tenant_licenses",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_iam_tenant_licenses_status",
                table: "iam_tenant_licenses",
                column: "Status");

            // ── AgentRegistration ──────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "iam_agent_registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    HostUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    HostName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AgentVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DeploymentMode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CpuCores = table.Column<int>(type: "integer", nullable: false),
                    RamGb = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    HostUnits = table.Column<decimal>(type: "numeric(6,1)", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastHeartbeatAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iam_agent_registrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "uix_iam_agent_registrations_tenant_host",
                table: "iam_agent_registrations",
                columns: new[] { "TenantId", "HostUnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_iam_agent_registrations_tenant",
                table: "iam_agent_registrations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_iam_agent_registrations_status",
                table: "iam_agent_registrations",
                column: "Status");

            // ── AlertFiringRecord ──────────────────────────────────────────
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
                name: "ix_iam_alert_firing_tenant_fired",
                table: "iam_alert_firing_records",
                columns: new[] { "TenantId", "FiredAt" });

            migrationBuilder.CreateIndex(
                name: "ix_iam_alert_firing_tenant_status",
                table: "iam_alert_firing_records",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_iam_alert_firing_rule",
                table: "iam_alert_firing_records",
                column: "AlertRuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "iam_alert_firing_records");
            migrationBuilder.DropTable(name: "iam_agent_registrations");
            migrationBuilder.DropTable(name: "iam_tenant_licenses");
        }
    }
}
