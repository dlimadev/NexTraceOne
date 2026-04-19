using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalKeyToRelease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adiciona a chave natural externa à tabela de releases.
            // Permite que consumidores externos (Jenkins, GitHub, Azure DevOps) consultem
            // e correlacionem releases pelo seu próprio identificador sem conhecerem o GUID
            // interno do NexTraceOne — implementa o padrão "Natural Key Routing".
            migrationBuilder.AddColumn<string>(
                name: "ExternalReleaseId",
                table: "chg_releases",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSystem",
                table: "chg_releases",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // Índice parcial sobre a chave natural — só indexa registos que tenham ambos os campos.
            // Partial index reduz overhead para releases internas que não têm origem externa.
            migrationBuilder.CreateIndex(
                name: "ix_chg_releases_external_key",
                table: "chg_releases",
                columns: new[] { "ExternalReleaseId", "ExternalSystem" },
                filter: "\"ExternalReleaseId\" IS NOT NULL AND \"ExternalSystem\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_chg_releases_external_key",
                table: "chg_releases");

            migrationBuilder.DropColumn(
                name: "ExternalReleaseId",
                table: "chg_releases");

            migrationBuilder.DropColumn(
                name: "ExternalSystem",
                table: "chg_releases");
        }
    }
}
