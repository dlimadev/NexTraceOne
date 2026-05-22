using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetDeploymentState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cat_asset_deployment_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ImageTag = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReleaseName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RuntimeStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastHeartbeatAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeployedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_asset_deployment_states", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cat_asset_deployment_states_DeployedAt",
                table: "cat_asset_deployment_states",
                column: "DeployedAt",
                descending: new[] { true });

            migrationBuilder.CreateIndex(
                name: "IX_cat_asset_deployment_states_IsDeleted",
                table: "cat_asset_deployment_states",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_cat_asset_deployment_states_ServiceAssetId_Environment",
                table: "cat_asset_deployment_states",
                columns: new[] { "ServiceAssetId", "Environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_asset_deployment_states_TenantId",
                table: "cat_asset_deployment_states",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cat_asset_deployment_states");
        }
    }
}
