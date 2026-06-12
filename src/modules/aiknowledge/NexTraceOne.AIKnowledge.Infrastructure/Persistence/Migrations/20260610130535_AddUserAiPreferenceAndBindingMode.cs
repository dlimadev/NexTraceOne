using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAiPreferenceAndBindingMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "FeatureModelBindings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "UserAiPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureKey = table.Column<string>(type: "text", nullable: false),
                    PreferenceType = table.Column<int>(type: "integer", nullable: false),
                    PreferredModelId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreferredProviderId = table.Column<string>(type: "text", nullable: true),
                    ExternalProduct = table.Column<int>(type: "integer", nullable: true),
                    ExternalProductModel = table.Column<string>(type: "text", nullable: true),
                    DisableReason = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAiPreferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAiPreferences_UserId_TenantId_FeatureKey",
                table: "UserAiPreferences",
                columns: new[] { "UserId", "TenantId", "FeatureKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserAiPreferences_UserId_TenantId_FeatureKey",
                table: "UserAiPreferences");

            migrationBuilder.DropTable(
                name: "UserAiPreferences");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "FeatureModelBindings");
        }
    }
}
