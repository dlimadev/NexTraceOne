using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations;

/// <summary>
/// Wave V3.4 — AI-assisted Dashboard Creation &amp; Notebook Mode.
/// Adiciona:
///   - Tabela gov_notebooks (Notebook aggregate com células JSONB e SharingPolicy)
/// </summary>
public partial class V34_NotebooksAndAiComposer : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "gov_notebooks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                TeamId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Persona = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                SharingPolicyJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{\"scope\":\"Private\",\"permission\":\"Read\"}"),
                CellsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                CurrentRevisionNumber = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                LinkedDashboardId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_gov_notebooks", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_gov_notebooks_tenant_status",
            table: "gov_notebooks",
            columns: new[] { "tenant_id", "Status" });

        migrationBuilder.CreateIndex(
            name: "ix_gov_notebooks_tenant_persona",
            table: "gov_notebooks",
            columns: new[] { "tenant_id", "Persona" });

        migrationBuilder.CreateIndex(
            name: "ix_gov_notebooks_created_by",
            table: "gov_notebooks",
            column: "CreatedByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "gov_notebooks");
    }
}
