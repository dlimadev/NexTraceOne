using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.DeveloperExperience.Persistence.Migrations;

/// <summary>
/// Wave AK.1 — IDE Context API.
///
/// Cria a tabela dx_ide_usage_records para persistir registos de uso da extensão IDE
/// por utilizador. Substitui o NullIDEUsageRepository que descartava silenciosamente
/// todos os eventos de uso.
/// </summary>
public partial class AddIdeUsageRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS dx_ide_usage_records (
                ""Id""           uuid         NOT NULL PRIMARY KEY,
                ""UserId""       varchar(200) NOT NULL,
                ""TenantId""     varchar(200) NOT NULL,
                ""EventType""    integer      NOT NULL,
                ""ResourceName"" varchar(500),
                ""OccurredAt""   timestamptz  NOT NULL
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_dx_iru_tenant
                ON dx_ide_usage_records (""TenantId"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_dx_iru_user_occurred
                ON dx_ide_usage_records (""UserId"", ""OccurredAt"" DESC);
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_dx_iru_tenant_occurred
                ON dx_ide_usage_records (""TenantId"", ""OccurredAt"" DESC);
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS dx_ide_usage_records;");
    }
}
