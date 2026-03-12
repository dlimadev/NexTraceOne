using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.EngineeringGraph.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialEngineeringGraphSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eg_consumer_assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eg_consumer_assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "eg_service_assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Domain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TeamName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eg_service_assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "eg_api_assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RoutePattern = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OwnerServiceId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eg_api_assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_eg_api_assets_eg_service_assets_OwnerServiceId",
                        column: x => x.OwnerServiceId,
                        principalTable: "eg_service_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "eg_consumer_relationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    FirstObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eg_consumer_relationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_eg_consumer_relationships_eg_api_assets_ApiAssetId",
                        column: x => x.ApiAssetId,
                        principalTable: "eg_api_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "eg_discovery_sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DiscoveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    ApiAssetId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eg_discovery_sources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_eg_discovery_sources_eg_api_assets_ApiAssetId",
                        column: x => x.ApiAssetId,
                        principalTable: "eg_api_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_eg_api_assets_OwnerServiceId",
                table: "eg_api_assets",
                column: "OwnerServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_eg_consumer_relationships_ApiAssetId",
                table: "eg_consumer_relationships",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_eg_discovery_sources_ApiAssetId",
                table: "eg_discovery_sources",
                column: "ApiAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_eg_service_assets_Name",
                table: "eg_service_assets",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_CreatedAt",
                table: "outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt",
                table: "outbox_messages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eg_consumer_assets");

            migrationBuilder.DropTable(
                name: "eg_consumer_relationships");

            migrationBuilder.DropTable(
                name: "eg_discovery_sources");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "eg_api_assets");

            migrationBuilder.DropTable(
                name: "eg_service_assets");
        }
    }
}
