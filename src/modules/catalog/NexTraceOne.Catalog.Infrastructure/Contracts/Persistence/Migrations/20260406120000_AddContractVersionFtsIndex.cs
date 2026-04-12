using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations;

/// <summary>
/// Adiciona um índice GIN de full-text search sobre a tabela ctr_contract_versions.
/// O índice cobre os campos SemVer, ImportedFrom e Protocol para acelerar pesquisas textuais
/// sem necessidade de geração de coluna computada persistida pelo EF Core.
/// </summary>
public partial class AddContractVersionFtsIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            @"CREATE INDEX IF NOT EXISTS ""IX_ctr_contract_versions_fts"" 
              ON ""ctr_contract_versions"" 
              USING GIN (to_tsvector('simple', coalesce(""SemVer"", '') || ' ' || coalesce(""ImportedFrom"", '') || ' ' || coalesce(""Protocol""::text, '')))");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_ctr_contract_versions_fts""");
    }
}
