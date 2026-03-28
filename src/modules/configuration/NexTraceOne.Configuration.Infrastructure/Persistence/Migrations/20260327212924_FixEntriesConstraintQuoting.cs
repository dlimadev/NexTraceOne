using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixEntriesConstraintQuoting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_cfg_entries_scope",
                table: "cfg_entries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_cfg_entries_version_positive",
                table: "cfg_entries");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cfg_entries_scope",
                table: "cfg_entries",
                sql: "\"Scope\" IN ('System', 'Tenant', 'Environment', 'Role', 'Team', 'User')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cfg_entries_version_positive",
                table: "cfg_entries",
                sql: "\"Version\" >= 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_cfg_entries_scope",
                table: "cfg_entries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_cfg_entries_version_positive",
                table: "cfg_entries");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cfg_entries_scope",
                table: "cfg_entries",
                sql: "scope IN ('System', 'Tenant', 'Environment', 'Role', 'Team', 'User')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cfg_entries_version_positive",
                table: "cfg_entries",
                sql: "version >= 1");
        }
    }
}
