using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cat_framework_asset_details",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Language = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PackageManager = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ArtifactRegistryUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    LatestVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    MinSupportedVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    TargetPlatform = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    LicenseType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    BuildPipelineUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    ChangelogUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    KnownConsumerCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_framework_asset_details", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cat_framework_asset_details_Language",
                table: "cat_framework_asset_details",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_cat_framework_asset_details_PackageName",
                table: "cat_framework_asset_details",
                column: "PackageName");

            migrationBuilder.CreateIndex(
                name: "IX_cat_framework_asset_details_ServiceAssetId",
                table: "cat_framework_asset_details",
                column: "ServiceAssetId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cat_framework_asset_details");
        }
    }
}
