using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CG_AddSlsaProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adiciona campos SLSA Level 3 à tabela de releases:
            // - slsa_provenance_uri: URI do documento de proveniência (GitHub Actions, Sigstore, etc.)
            // - artifact_digest: digest criptográfico do artefacto (algo:hex, e.g. sha256:abc123...)
            // - sbom_uri: URI do SBOM em CycloneDX ou SPDX
            migrationBuilder.AddColumn<string>(
                name: "slsa_provenance_uri",
                table: "chg_releases",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "artifact_digest",
                table: "chg_releases",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sbom_uri",
                table: "chg_releases",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            // Índice parcial — permite pesquisa eficiente de releases com artifact attestation
            migrationBuilder.CreateIndex(
                name: "ix_chg_releases_artifact_digest",
                table: "chg_releases",
                column: "artifact_digest",
                filter: "\"artifact_digest\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_chg_releases_artifact_digest",
                table: "chg_releases");

            migrationBuilder.DropColumn(name: "slsa_provenance_uri", table: "chg_releases");
            migrationBuilder.DropColumn(name: "artifact_digest", table: "chg_releases");
            migrationBuilder.DropColumn(name: "sbom_uri", table: "chg_releases");
        }
    }
}
