using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConsumedServicesJson",
                table: "ctr_background_service_draft_metadata",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ConsumedTopicsJson",
                table: "ctr_background_service_draft_metadata",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "MessagingRole",
                table: "ctr_background_service_draft_metadata",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "ProducedEventsJson",
                table: "ctr_background_service_draft_metadata",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ProducedTopicsJson",
                table: "ctr_background_service_draft_metadata",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ConsumedServicesJson",
                table: "ctr_background_service_contract_details",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ConsumedTopicsJson",
                table: "ctr_background_service_contract_details",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "MessagingRole",
                table: "ctr_background_service_contract_details",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "ProducedEventsJson",
                table: "ctr_background_service_contract_details",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ProducedTopicsJson",
                table: "ctr_background_service_contract_details",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "ctr_canonical_entity_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CanonicalEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SchemaContent = table.Column<string>(type: "text", nullable: false),
                    SchemaFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangeDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PublishedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_canonical_entity_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ctr_consumer_expectations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConsumerDomain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExpectedSubsetJson = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ctr_consumer_expectations", x => x.Id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ctr_bg_service_draft_messaging_role",
                table: "ctr_background_service_draft_metadata",
                sql: "\"MessagingRole\" IN ('None', 'Producer', 'Consumer', 'Both')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ctr_bg_service_details_messaging_role",
                table: "ctr_background_service_contract_details",
                sql: "\"MessagingRole\" IN ('None', 'Producer', 'Consumer', 'Both')");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_canonical_entity_versions_CanonicalEntityId",
                table: "ctr_canonical_entity_versions",
                column: "CanonicalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_canonical_entity_versions_CanonicalEntityId_Version",
                table: "ctr_canonical_entity_versions",
                columns: new[] { "CanonicalEntityId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_consumer_expectations_ApiAssetId",
                table: "ctr_consumer_expectations",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ctr_consumer_expectations_ApiAssetId_ConsumerServiceName",
                table: "ctr_consumer_expectations",
                columns: new[] { "ApiAssetId", "ConsumerServiceName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ctr_consumer_expectations_IsActive",
                table: "ctr_consumer_expectations",
                column: "IsActive",
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ctr_canonical_entity_versions");

            migrationBuilder.DropTable(
                name: "ctr_consumer_expectations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ctr_bg_service_draft_messaging_role",
                table: "ctr_background_service_draft_metadata");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ctr_bg_service_details_messaging_role",
                table: "ctr_background_service_contract_details");

            migrationBuilder.DropColumn(
                name: "ConsumedServicesJson",
                table: "ctr_background_service_draft_metadata");

            migrationBuilder.DropColumn(
                name: "ConsumedTopicsJson",
                table: "ctr_background_service_draft_metadata");

            migrationBuilder.DropColumn(
                name: "MessagingRole",
                table: "ctr_background_service_draft_metadata");

            migrationBuilder.DropColumn(
                name: "ProducedEventsJson",
                table: "ctr_background_service_draft_metadata");

            migrationBuilder.DropColumn(
                name: "ProducedTopicsJson",
                table: "ctr_background_service_draft_metadata");

            migrationBuilder.DropColumn(
                name: "ConsumedServicesJson",
                table: "ctr_background_service_contract_details");

            migrationBuilder.DropColumn(
                name: "ConsumedTopicsJson",
                table: "ctr_background_service_contract_details");

            migrationBuilder.DropColumn(
                name: "MessagingRole",
                table: "ctr_background_service_contract_details");

            migrationBuilder.DropColumn(
                name: "ProducedEventsJson",
                table: "ctr_background_service_contract_details");

            migrationBuilder.DropColumn(
                name: "ProducedTopicsJson",
                table: "ctr_background_service_contract_details");
        }
    }
}
