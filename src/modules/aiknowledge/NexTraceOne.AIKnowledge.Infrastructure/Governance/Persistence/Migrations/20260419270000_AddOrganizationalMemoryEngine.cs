using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// Fase 11 — Organizational Memory Engine para conhecimento organizacional.
/// Cria tabela aik_memory_nodes.
/// </summary>
public partial class AddOrganizationalMemoryEngine : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_memory_nodes (
                ""Id""                 uuid             NOT NULL PRIMARY KEY,
                ""NodeType""           varchar(100)     NOT NULL,
                ""Subject""            varchar(500)     NOT NULL,
                ""Title""              varchar(500)     NOT NULL,
                ""Content""            text,
                ""Context""            varchar(2000),
                ""ActorId""            varchar(200),
                ""TagsJson""           text             NOT NULL DEFAULT '[]',
                ""LinkedNodeIdsJson""  text             NOT NULL DEFAULT '[]',
                ""SourceType""         varchar(100),
                ""SourceId""           varchar(200),
                ""RelevanceScore""     double precision NOT NULL DEFAULT 1.0,
                ""TenantId""           uuid             NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                ""RecordedAt""         timestamptz      NOT NULL,
                ""CreatedAt""          timestamptz,
                ""CreatedBy""          varchar(500),
                ""UpdatedAt""          timestamptz,
                ""UpdatedBy""          varchar(500)
            );
        ");

        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_memory_tenant ON aik_memory_nodes (""TenantId"");");
        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_memory_subject ON aik_memory_nodes (""Subject"", ""TenantId"");");
        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_memory_node_type ON aik_memory_nodes (""NodeType"");");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_memory_nodes;");
    }
}
