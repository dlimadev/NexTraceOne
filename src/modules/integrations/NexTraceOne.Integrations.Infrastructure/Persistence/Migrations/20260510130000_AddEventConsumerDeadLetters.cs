using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adiciona a tabela int_event_consumer_dead_letters para persistência de eventos
/// que falharam o processamento no consumer worker.
///
/// Substitui o NullEventConsumerDeadLetterRepository que armazenava registos em
/// ConcurrentDictionary (memória perdida ao reiniciar). Permite reprocessamento manual
/// e auditoria de falhas de ingestão.
/// </summary>
public partial class AddEventConsumerDeadLetters : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS int_event_consumer_dead_letters (
                ""Id""              uuid            NOT NULL PRIMARY KEY,
                ""TenantId""        uuid            NOT NULL,
                ""SourceType""      varchar(100)    NOT NULL,
                ""Topic""           varchar(500)    NOT NULL,
                ""PartitionKey""    varchar(500),
                ""Payload""         text            NOT NULL,
                ""AttemptCount""    integer         NOT NULL DEFAULT 1,
                ""LastError""       varchar(4000)   NOT NULL,
                ""FirstAttemptAt""  timestamptz     NOT NULL,
                ""LastAttemptAt""   timestamptz     NOT NULL,
                ""IsResolved""      boolean         NOT NULL DEFAULT false,
                ""ResolvedAt""      timestamptz
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_int_ecdl_tenant
                ON int_event_consumer_dead_letters (""TenantId"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_int_ecdl_resolved
                ON int_event_consumer_dead_letters (""IsResolved"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_int_ecdl_tenant_resolved
                ON int_event_consumer_dead_letters (""TenantId"", ""IsResolved"");
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS int_event_consumer_dead_letters;");
    }
}
