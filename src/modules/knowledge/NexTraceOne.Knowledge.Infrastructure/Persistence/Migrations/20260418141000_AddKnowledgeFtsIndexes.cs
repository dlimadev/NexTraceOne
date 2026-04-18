using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Knowledge.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adiciona índices GIN de full-text search para os módulos Knowledge.
    /// Cobre: knw_documents (Title + Content + Summary) e knw_operational_notes (Content + Title).
    /// Resolve gap GAPS-BANCO-DE-DADOS.md §4 — FTS parcial: Knowledge sem cobertura.
    /// </summary>
    public partial class AddKnowledgeFtsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FTS index for KnowledgeDocument — pesquisa em título, conteúdo e sumário
            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_knw_documents_fts""
                  ON ""knw_documents""
                  USING GIN (
                    to_tsvector('simple',
                      coalesce(""Title"", '') || ' ' ||
                      coalesce(""Summary"", '') || ' ' ||
                      coalesce(""Content"", '')
                    )
                  )");

            // FTS index for OperationalNote — pesquisa em título e conteúdo
            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_knw_operational_notes_fts""
                  ON ""knw_operational_notes""
                  USING GIN (
                    to_tsvector('simple',
                      coalesce(""Title"", '') || ' ' ||
                      coalesce(""Content"", '')
                    )
                  )");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_knw_documents_fts""");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_knw_operational_notes_fts""");
        }
    }
}
