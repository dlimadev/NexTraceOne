using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
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
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, defaultValue: ""),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    ServiceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "RestApi"),
                    Domain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SystemArea = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    TeamName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TechnicalOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    BusinessOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    Criticality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Medium"),
                    LifecycleStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    ExposureType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Internal"),
                    DocumentationUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    RepositoryUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eg_service_assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "graph_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NodesJson = table.Column<string>(type: "text", nullable: false),
                    EdgesJson = table.Column<string>(type: "text", nullable: false),
                    NodeCount = table.Column<int>(type: "integer", nullable: false),
                    EdgeCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_graph_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "node_health_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OverlayMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    FactorsJson = table.Column<string>(type: "text", nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_node_health_records", x => x.Id);
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
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "saved_graph_views",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsShared = table.Column<bool>(type: "boolean", nullable: false),
                    FiltersJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_graph_views", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sot_linked_references",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetType = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                    ReferenceType = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sot_linked_references", x => x.Id);
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
                    OwnerServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDecommissioned = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "IX_eg_service_assets_Criticality",
                table: "eg_service_assets",
                column: "Criticality");

            migrationBuilder.CreateIndex(
                name: "IX_eg_service_assets_Domain",
                table: "eg_service_assets",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_eg_service_assets_LifecycleStatus",
                table: "eg_service_assets",
                column: "LifecycleStatus");

            migrationBuilder.CreateIndex(
                name: "IX_eg_service_assets_Name",
                table: "eg_service_assets",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_eg_service_assets_ServiceType",
                table: "eg_service_assets",
                column: "ServiceType");

            migrationBuilder.CreateIndex(
                name: "IX_eg_service_assets_TeamName",
                table: "eg_service_assets",
                column: "TeamName");

            migrationBuilder.CreateIndex(
                name: "IX_graph_snapshots_CapturedAt",
                table: "graph_snapshots",
                column: "CapturedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_node_health_records_CalculatedAt",
                table: "node_health_records",
                column: "CalculatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_node_health_records_NodeId_OverlayMode",
                table: "node_health_records",
                columns: new[] { "NodeId", "OverlayMode" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_CreatedAt",
                table: "outbox_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_IdempotencyKey",
                table: "outbox_messages",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt",
                table: "outbox_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_saved_graph_views_OwnerId",
                table: "saved_graph_views",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_saved_graph_views_OwnerId_IsShared",
                table: "saved_graph_views",
                columns: new[] { "OwnerId", "IsShared" });

            migrationBuilder.CreateIndex(
                name: "IX_sot_linked_references_AssetId_AssetType",
                table: "sot_linked_references",
                columns: new[] { "AssetId", "AssetType" });

            migrationBuilder.CreateIndex(
                name: "IX_sot_linked_references_ReferenceType",
                table: "sot_linked_references",
                column: "ReferenceType");
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
                name: "graph_snapshots");

            migrationBuilder.DropTable(
                name: "node_health_records");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "saved_graph_views");

            migrationBuilder.DropTable(
                name: "sot_linked_references");

            migrationBuilder.DropTable(
                name: "eg_api_assets");

            migrationBuilder.DropTable(
                name: "eg_service_assets");
        }
    }
}
