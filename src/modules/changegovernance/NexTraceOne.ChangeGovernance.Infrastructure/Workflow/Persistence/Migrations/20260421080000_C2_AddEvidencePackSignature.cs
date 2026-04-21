using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class C2_AddEvidencePackSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntegrityHash",
                table: "chg_evidence_packs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrityManifest",
                table: "chg_evidence_packs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "IntegritySignedAt",
                table: "chg_evidence_packs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegritySignedBy",
                table: "chg_evidence_packs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntegrityHash",
                table: "chg_evidence_packs");

            migrationBuilder.DropColumn(
                name: "IntegrityManifest",
                table: "chg_evidence_packs");

            migrationBuilder.DropColumn(
                name: "IntegritySignedAt",
                table: "chg_evidence_packs");

            migrationBuilder.DropColumn(
                name: "IntegritySignedBy",
                table: "chg_evidence_packs");
        }
    }
}
