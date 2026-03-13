using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.EngineeringGraph.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelChanges_EngineeringGraph : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDecommissioned",
                table: "eg_api_assets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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
                name: "IX_saved_graph_views_OwnerId",
                table: "saved_graph_views",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_saved_graph_views_OwnerId_IsShared",
                table: "saved_graph_views",
                columns: new[] { "OwnerId", "IsShared" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "graph_snapshots");

            migrationBuilder.DropTable(
                name: "node_health_records");

            migrationBuilder.DropTable(
                name: "saved_graph_views");

            migrationBuilder.DropColumn(
                name: "IsDecommissioned",
                table: "eg_api_assets");
        }
    }
}
