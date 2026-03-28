using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixCheckConstraintQuoting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_cfg_feature_flag_entries_scope",
                table: "cfg_feature_flag_entries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_cfg_definitions_category",
                table: "cfg_definitions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_cfg_definitions_value_type",
                table: "cfg_definitions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cfg_feature_flag_entries_scope",
                table: "cfg_feature_flag_entries",
                sql: "\"Scope\" IN ('System', 'Tenant', 'Environment', 'Role', 'Team', 'User')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cfg_definitions_category",
                table: "cfg_definitions",
                sql: "\"Category\" IN ('Bootstrap', 'SensitiveOperational', 'Functional')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cfg_definitions_value_type",
                table: "cfg_definitions",
                sql: "\"ValueType\" IN ('String', 'Integer', 'Decimal', 'Boolean', 'Json', 'StringList')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_cfg_feature_flag_entries_scope",
                table: "cfg_feature_flag_entries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_cfg_definitions_category",
                table: "cfg_definitions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_cfg_definitions_value_type",
                table: "cfg_definitions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cfg_feature_flag_entries_scope",
                table: "cfg_feature_flag_entries",
                sql: "scope IN ('System', 'Tenant', 'Environment', 'Role', 'Team', 'User')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cfg_definitions_category",
                table: "cfg_definitions",
                sql: "category IN ('Bootstrap', 'SensitiveOperational', 'Functional')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cfg_definitions_value_type",
                table: "cfg_definitions",
                sql: "value_type IN ('String', 'Integer', 'Decimal', 'Boolean', 'Json', 'StringList')");
        }
    }
}
