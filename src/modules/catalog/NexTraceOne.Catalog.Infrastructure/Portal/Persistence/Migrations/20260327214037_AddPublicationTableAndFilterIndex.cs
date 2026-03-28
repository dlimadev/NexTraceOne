using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicationTableAndFilterIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cat_portal_contract_publications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SemVer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Visibility = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PublishedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReleaseNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    WithdrawnBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WithdrawnAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WithdrawalReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cat_portal_contract_publications", x => x.Id);
                    table.CheckConstraint("CK_cat_portal_contract_publications_status", "\"Status\" IN ('PendingPublication', 'Published', 'Withdrawn', 'Deprecated')");
                    table.CheckConstraint("CK_cat_portal_contract_publications_visibility", "\"Visibility\" IN ('Internal', 'External', 'RestrictedToTeams')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_contract_publications_ApiAssetId",
                table: "cat_portal_contract_publications",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_contract_publications_ContractVersionId",
                table: "cat_portal_contract_publications",
                column: "ContractVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_contract_publications_IsDeleted",
                table: "cat_portal_contract_publications",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_cat_portal_contract_publications_Status",
                table: "cat_portal_contract_publications",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cat_portal_contract_publications");
        }
    }
}
