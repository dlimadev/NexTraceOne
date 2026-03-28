using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BuildId",
                table: "chg_evidence_packs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CiChecksResult",
                table: "chg_evidence_packs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommitSha",
                table: "chg_evidence_packs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PipelineSource",
                table: "chg_evidence_packs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildId",
                table: "chg_evidence_packs");

            migrationBuilder.DropColumn(
                name: "CiChecksResult",
                table: "chg_evidence_packs");

            migrationBuilder.DropColumn(
                name: "CommitSha",
                table: "chg_evidence_packs");

            migrationBuilder.DropColumn(
                name: "PipelineSource",
                table: "chg_evidence_packs");
        }
    }
}
