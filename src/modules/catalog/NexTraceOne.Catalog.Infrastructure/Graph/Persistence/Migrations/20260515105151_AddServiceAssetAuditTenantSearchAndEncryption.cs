using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceAssetAuditTenantSearchAndEncryption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_cat_service_assets_exposure_type",
                table: "cat_service_assets");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "cat_service_assets",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "cat_service_assets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "cat_service_assets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "cat_service_assets",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "simple")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Name", "DisplayName", "Domain", "TeamName", "Description" });

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "cat_service_assets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "cat_service_assets",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "cat_service_assets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_cat_service_assets_SearchVector",
                table: "cat_service_assets",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cat_service_assets_exposure_type",
                table: "cat_service_assets",
                sql: "\"ExposureType\" IN ('Internal', 'External', 'Partner')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cat_service_assets_SearchVector",
                table: "cat_service_assets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_cat_service_assets_exposure_type",
                table: "cat_service_assets");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "cat_service_assets");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "cat_service_assets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "cat_service_assets");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "cat_service_assets");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "cat_service_assets");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "cat_service_assets");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "cat_service_assets");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cat_service_assets_exposure_type",
                table: "cat_service_assets",
                sql: "\"ExposureType\" IN ('Internal', 'Partner', 'Public')");
        }
    }
}
