using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Knowledge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeGraphSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "knw_knowledge_graph_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalNodes = table.Column<int>(type: "integer", nullable: false),
                    TotalEdges = table.Column<int>(type: "integer", nullable: false),
                    ConnectedComponents = table.Column<int>(type: "integer", nullable: false),
                    IsolatedNodes = table.Column<int>(type: "integer", nullable: false),
                    CoverageScore = table.Column<int>(type: "integer", nullable: false),
                    NodeTypeDistribution = table.Column<string>(type: "jsonb", nullable: false),
                    EdgeTypeDistribution = table.Column<string>(type: "jsonb", nullable: false),
                    TopConnectedEntities = table.Column<string>(type: "jsonb", nullable: true),
                    OrphanEntities = table.Column<string>(type: "jsonb", nullable: true),
                    Recommendations = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knw_knowledge_graph_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_knw_knowledge_graph_snapshots_GeneratedAt",
                table: "knw_knowledge_graph_snapshots",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_knw_knowledge_graph_snapshots_Status",
                table: "knw_knowledge_graph_snapshots",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_knw_knowledge_graph_snapshots_tenant_id",
                table: "knw_knowledge_graph_snapshots",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "knw_knowledge_graph_snapshots");
        }
    }
}
