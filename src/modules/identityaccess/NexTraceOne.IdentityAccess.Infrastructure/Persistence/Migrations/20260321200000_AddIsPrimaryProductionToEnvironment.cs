using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Migrations;

/// <summary>
/// Migração: AddIsPrimaryProductionToEnvironment
///
/// Adiciona o campo IsPrimaryProduction à tabela identity_environments.
/// Garante que somente um ambiente ativo com IsPrimaryProduction=true pode existir por tenant
/// por meio de um índice único parcial.
///
/// Regra de negócio: cada tenant deve ter no máximo um ambiente designado como produção principal ativo.
/// O índice parcial filtra apenas linhas onde IsPrimaryProduction=true AND IsActive=true,
/// permitindo múltiplos ambientes sem a flag ou inativos sem conflito.
/// </summary>
public partial class AddIsPrimaryProductionToEnvironment : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsPrimaryProduction",
            table: "identity_environments",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        // Índice único parcial: garante unicidade de ambiente produtivo principal ativo por tenant.
        // O filtro "IsPrimaryProduction = true AND IsActive = true" significa que somente
        // registos com ambas as flags true competem pela unicidade no índice.
        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX "IX_identity_environments_tenant_primary_production_unique"
            ON "identity_environments" ("TenantId", "IsPrimaryProduction")
            WHERE "IsPrimaryProduction" = true AND "IsActive" = true;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_identity_environments_tenant_primary_production_unique",
            table: "identity_environments");

        migrationBuilder.DropColumn(
            name: "IsPrimaryProduction",
            table: "identity_environments");
    }
}
