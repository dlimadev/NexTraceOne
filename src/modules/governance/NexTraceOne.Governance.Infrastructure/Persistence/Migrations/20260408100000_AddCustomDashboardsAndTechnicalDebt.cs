using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adiciona tabelas para Custom Dashboards e Technical Debt Items.
/// Estas entidades passam de dados hardcoded/simulados para persistência real em PostgreSQL.
/// </summary>
public partial class AddCustomDashboardsAndTechnicalDebt : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── gov_custom_dashboards ─────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "gov_custom_dashboards",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Layout = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Persona = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                WidgetIds = table.Column<string>(type: "jsonb", nullable: false),
                IsShared = table.Column<bool>(type: "boolean", nullable: false),
                tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gov_custom_dashboards", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_gov_custom_dashboards_tenant_id",
            table: "gov_custom_dashboards",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "IX_gov_custom_dashboards_Persona",
            table: "gov_custom_dashboards",
            column: "Persona");

        migrationBuilder.CreateIndex(
            name: "IX_gov_custom_dashboards_CreatedByUserId",
            table: "gov_custom_dashboards",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_gov_custom_dashboards_CreatedAt",
            table: "gov_custom_dashboards",
            column: "CreatedAt");

        // ── gov_technical_debt_items ──────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "gov_technical_debt_items",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                DebtType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                EstimatedEffortDays = table.Column<int>(type: "integer", nullable: false),
                DebtScore = table.Column<int>(type: "integer", nullable: false),
                Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gov_technical_debt_items", x => x.Id);
                table.CheckConstraint(
                    "CK_gov_technical_debt_items_severity",
                    "\"Severity\" IN ('critical', 'high', 'medium', 'low')");
                table.CheckConstraint(
                    "CK_gov_technical_debt_items_debt_type",
                    "\"DebtType\" IN ('architecture', 'code-quality', 'security', 'dependency', 'documentation', 'testing', 'performance', 'infrastructure')");
            });

        migrationBuilder.CreateIndex(
            name: "IX_gov_technical_debt_items_tenant_id",
            table: "gov_technical_debt_items",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "IX_gov_technical_debt_items_ServiceName",
            table: "gov_technical_debt_items",
            column: "ServiceName");

        migrationBuilder.CreateIndex(
            name: "IX_gov_technical_debt_items_DebtType",
            table: "gov_technical_debt_items",
            column: "DebtType");

        migrationBuilder.CreateIndex(
            name: "IX_gov_technical_debt_items_Severity",
            table: "gov_technical_debt_items",
            column: "Severity");

        migrationBuilder.CreateIndex(
            name: "IX_gov_technical_debt_items_CreatedAt",
            table: "gov_technical_debt_items",
            column: "CreatedAt");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "gov_technical_debt_items");
        migrationBuilder.DropTable(name: "gov_custom_dashboards");
    }
}
