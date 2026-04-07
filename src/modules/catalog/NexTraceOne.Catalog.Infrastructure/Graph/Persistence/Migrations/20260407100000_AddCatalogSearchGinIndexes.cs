using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Migrations;

/// <summary>
/// Adiciona índice GIN de full-text search sobre a tabela cat_service_assets (Name, DisplayName, Domain, TeamName)
/// e cat_api_assets (Name, RoutePattern) para acelerar pesquisas textuais no catálogo de serviços.
/// </summary>
public partial class AddCatalogSearchGinIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // GIN index para pesquisa de serviços por Name, DisplayName, Domain e TeamName
        migrationBuilder.Sql(
            @"CREATE INDEX IF NOT EXISTS ""IX_cat_service_assets_fts""
              ON ""cat_service_assets""
              USING GIN (to_tsvector('simple',
                  coalesce(""Name"", '') || ' ' ||
                  coalesce(""DisplayName"", '') || ' ' ||
                  coalesce(""Domain"", '') || ' ' ||
                  coalesce(""TeamName"", '')))");

        // GIN index para pesquisa de API assets por Name e RoutePattern
        migrationBuilder.Sql(
            @"CREATE INDEX IF NOT EXISTS ""IX_cat_api_assets_fts""
              ON ""cat_api_assets""
              USING GIN (to_tsvector('simple',
                  coalesce(""Name"", '') || ' ' ||
                  coalesce(""RoutePattern"", '')))");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_cat_service_assets_fts""");
        migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_cat_api_assets_fts""");
    }
}
