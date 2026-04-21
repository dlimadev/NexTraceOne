using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Knowledge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KNW_AddFreshnessAndProposedRunbook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Add freshness columns to knw_documents ──────────────────────
            migrationBuilder.AddColumn<int>(
                name: "freshness_score",
                table: "knw_documents",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_reviewed_at",
                table: "knw_documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reviewed_by",
                table: "knw_documents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // ── Create knw_proposed_runbooks ──────────────────────────────────
            migrationBuilder.CreateTable(
                name: "knw_proposed_runbooks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    content_markdown = table.Column<string>(type: "text", nullable: false),
                    source_incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    team_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    proposed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    review_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_knw_proposed_runbooks", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "uix_knw_proposed_runbooks_incident",
                table: "knw_proposed_runbooks",
                column: "source_incident_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "knw_proposed_runbooks");
            migrationBuilder.DropColumn(name: "freshness_score", table: "knw_documents");
            migrationBuilder.DropColumn(name: "last_reviewed_at", table: "knw_documents");
            migrationBuilder.DropColumn(name: "reviewed_by", table: "knw_documents");
        }
    }
}
