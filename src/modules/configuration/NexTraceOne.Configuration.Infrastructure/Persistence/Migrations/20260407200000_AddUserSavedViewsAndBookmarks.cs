using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSavedViewsAndBookmarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cfg_user_saved_views",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Context = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FiltersJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    IsShared = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_user_saved_views", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cfg_user_bookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfg_user_bookmarks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_saved_views_UserId",
                table: "cfg_user_saved_views",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_saved_views_UserId_Context",
                table: "cfg_user_saved_views",
                columns: new[] { "UserId", "Context" });

            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_saved_views_UserId_TenantId_Context_Name",
                table: "cfg_user_saved_views",
                columns: new[] { "UserId", "TenantId", "Context", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_saved_views_IsShared",
                table: "cfg_user_saved_views",
                column: "IsShared",
                filter: "\"IsShared\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_bookmarks_UserId",
                table: "cfg_user_bookmarks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_bookmarks_UserId_TenantId",
                table: "cfg_user_bookmarks",
                columns: new[] { "UserId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_cfg_user_bookmarks_UserId_TenantId_EntityType_EntityId",
                table: "cfg_user_bookmarks",
                columns: new[] { "UserId", "TenantId", "EntityType", "EntityId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "cfg_user_saved_views");
            migrationBuilder.DropTable(name: "cfg_user_bookmarks");
        }
    }
}
