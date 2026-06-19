using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Governance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomDashboardTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "gov_custom_dashboards",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tags",
                table: "gov_custom_dashboards");
        }
    }
}
