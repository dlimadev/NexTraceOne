using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aik_feature_model_bindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequiredProviderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FallbackModelId = table.Column<Guid>(type: "uuid", nullable: true),
                    FallbackModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FallbackProviderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aik_feature_model_bindings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aik_feature_model_bindings_FeatureKey_TenantId",
                table: "aik_feature_model_bindings",
                columns: new[] { "FeatureKey", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aik_feature_model_bindings_IsActive",
                table: "aik_feature_model_bindings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_aik_feature_model_bindings_TenantId",
                table: "aik_feature_model_bindings",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aik_feature_model_bindings");
        }
    }
}
