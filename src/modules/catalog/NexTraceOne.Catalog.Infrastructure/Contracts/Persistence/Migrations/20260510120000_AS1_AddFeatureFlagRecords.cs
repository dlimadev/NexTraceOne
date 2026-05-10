using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations;

/// <summary>
/// Wave AS.1 — Feature Flag &amp; Experimentation Governance.
///
/// Cria a tabela ctr_feature_flag_records para persistir o estado actual de feature
/// flags por serviço e tenant. Substitui o NullFeatureFlagRepository (singleton em Application)
/// que descartava silenciosamente todos os upserts de estado.
/// </summary>
public partial class AS1_AddFeatureFlagRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ctr_feature_flag_records (
                ""Id""                      uuid            NOT NULL PRIMARY KEY,
                ""TenantId""                varchar(200)    NOT NULL,
                ""ServiceId""               varchar(200)    NOT NULL,
                ""FlagKey""                 varchar(200)    NOT NULL,
                ""Type""                    varchar(50)     NOT NULL,
                ""IsEnabled""               boolean         NOT NULL DEFAULT false,
                ""EnabledEnvironmentsJson"" jsonb,
                ""OwnerId""                 varchar(200),
                ""CreatedAt""               timestamptz     NOT NULL,
                ""LastToggledAt""           timestamptz,
                ""ScheduledRemovalDate""    timestamptz,
                CONSTRAINT uq_ctr_ffr_tenant_service_key UNIQUE (""TenantId"", ""ServiceId"", ""FlagKey"")
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_ctr_ffr_tenant
                ON ctr_feature_flag_records (""TenantId"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_ctr_ffr_tenant_service
                ON ctr_feature_flag_records (""TenantId"", ""ServiceId"");
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS ctr_feature_flag_records;");
    }
}
