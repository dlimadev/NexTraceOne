using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceReleaseTenantIdNotNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove registos órfãos que não têm tenant associado.
            // Estes registos foram criados antes da Fase 4 (tenant context) e não têm
            // owner válido — não podem ser servidos corretamente num sistema multi-tenant.
            migrationBuilder.Sql(
                "DELETE FROM chg_releases WHERE tenant_id IS NULL;");

            // Torna a coluna obrigatória — todas as releases devem pertencer a um tenant.
            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "chg_releases",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                table: "chg_releases",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: false);
        }
    }
}
