using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations
{
    /// <summary>
    /// Migração que torna a coluna service_id obrigatória (NOT NULL) na tabela ctr_contract_drafts.
    /// Todos os drafts devem estar vinculados a um serviço do catálogo.
    /// </summary>
    public partial class AddNotNullServiceIdToContractDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill: descarta drafts sem serviço (ServiceId nulo) marcando-os como descartados.
            // Isso garante que não existam drafts órfãos ao tornar a coluna NOT NULL.
            migrationBuilder.Sql("""
                UPDATE ctr_contract_drafts
                SET "Status" = 'Discarded'
                WHERE "ServiceId" IS NULL AND "Status" NOT IN ('Published', 'Discarded');
                """);

            // Atribui um UUID de sentinela para registos nulos que eventualmente existam,
            // para satisfazer a constraint NOT NULL antes de aplicá-la.
            migrationBuilder.Sql("""
                UPDATE ctr_contract_drafts
                SET "ServiceId" = '00000000-0000-0000-0000-000000000000'
                WHERE "ServiceId" IS NULL;
                """);

            // Altera coluna service_id para NOT NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "ServiceId",
                table: "ctr_contract_drafts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid?),
                oldType: "uuid",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid?>(
                name: "ServiceId",
                table: "ctr_contract_drafts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: false);
        }
    }
}
