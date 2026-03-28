using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixIndexFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cfg_modules_IsActive",
                table: "cfg_modules");

            migrationBuilder.DropIndex(
                name: "IX_cfg_feature_flag_entries_IsActive",
                table: "cfg_feature_flag_entries");

            migrationBuilder.DropIndex(
                name: "IX_cfg_feature_flag_definitions_IsActive",
                table: "cfg_feature_flag_definitions");

            migrationBuilder.DropIndex(
                name: "IX_cfg_entries_IsActive",
                table: "cfg_entries");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_modules_IsActive",
                table: "cfg_modules",
                column: "IsActive",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_entries_IsActive",
                table: "cfg_feature_flag_entries",
                column: "IsActive",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_definitions_IsActive",
                table: "cfg_feature_flag_definitions",
                column: "IsActive",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_entries_IsActive",
                table: "cfg_entries",
                column: "IsActive",
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cfg_modules_IsActive",
                table: "cfg_modules");

            migrationBuilder.DropIndex(
                name: "IX_cfg_feature_flag_entries_IsActive",
                table: "cfg_feature_flag_entries");

            migrationBuilder.DropIndex(
                name: "IX_cfg_feature_flag_definitions_IsActive",
                table: "cfg_feature_flag_definitions");

            migrationBuilder.DropIndex(
                name: "IX_cfg_entries_IsActive",
                table: "cfg_entries");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_modules_IsActive",
                table: "cfg_modules",
                column: "IsActive",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_entries_IsActive",
                table: "cfg_feature_flag_entries",
                column: "IsActive",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_definitions_IsActive",
                table: "cfg_feature_flag_definitions",
                column: "IsActive",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_entries_IsActive",
                table: "cfg_entries",
                column: "IsActive",
                filter: "\"IsActive\" = true");
        }
    }
}
