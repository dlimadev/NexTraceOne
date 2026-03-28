using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ModuleId",
                table: "cfg_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cfg_modules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_modules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_feature_flag_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DefaultEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AllowedScopes = table.Column<string[]>(type: "text[]", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsEditable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_feature_flag_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cfg_feature_flag_definitions_cfg_modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "cfg_modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "cfg_feature_flag_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScopeReferenceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_feature_flag_entries", x => x.Id);
                    table.CheckConstraint("CK_cfg_feature_flag_entries_scope", "\"Scope\" IN ('System', 'Tenant', 'Environment', 'Role', 'Team', 'User')");
                    table.ForeignKey(
                        name: "FK_cfg_feature_flag_entries_cfg_feature_flag_definitions_Defin~",
                        column: x => x.DefinitionId,
                        principalTable: "cfg_feature_flag_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cfg_definitions_ModuleId",
                table: "cfg_definitions",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_definitions_IsActive",
                table: "cfg_feature_flag_definitions",
                column: "IsActive",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_definitions_Key",
                table: "cfg_feature_flag_definitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_definitions_ModuleId",
                table: "cfg_feature_flag_definitions",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_entries_DefinitionId",
                table: "cfg_feature_flag_entries",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_entries_IsActive",
                table: "cfg_feature_flag_entries",
                column: "IsActive",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_entries_Key",
                table: "cfg_feature_flag_entries",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_entries_Key_Scope_ScopeReferenceId",
                table: "cfg_feature_flag_entries",
                columns: new[] { "Key", "Scope", "ScopeReferenceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfg_feature_flag_entries_Scope",
                table: "cfg_feature_flag_entries",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_modules_IsActive",
                table: "cfg_modules",
                column: "IsActive",
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_modules_Key",
                table: "cfg_modules",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfg_modules_SortOrder",
                table: "cfg_modules",
                column: "SortOrder");

            migrationBuilder.AddForeignKey(
                name: "FK_cfg_definitions_cfg_modules_ModuleId",
                table: "cfg_definitions",
                column: "ModuleId",
                principalTable: "cfg_modules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cfg_definitions_cfg_modules_ModuleId",
                table: "cfg_definitions");

            migrationBuilder.DropTable(
                name: "cfg_feature_flag_entries");

            migrationBuilder.DropTable(
                name: "cfg_feature_flag_definitions");

            migrationBuilder.DropTable(
                name: "cfg_modules");

            migrationBuilder.DropIndex(
                name: "IX_cfg_definitions_ModuleId",
                table: "cfg_definitions");

            migrationBuilder.DropColumn(
                name: "ModuleId",
                table: "cfg_definitions");
        }
    }
}
