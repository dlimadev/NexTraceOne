using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Migrations;

/// <summary>
/// Wave AO.1 / AQ.1 / AV.3 — Supply Chain, Data Contract &amp; Deprecation Schedule persistence.
///
/// Cria três tabelas que substituem as NullRepository stubs que descartavam
/// silenciosamente todos os dados ingeridos:
///
///   ctr_sbom_records              — Bill of Materials por serviço/versão com componentes jsonb
///   ctr_data_contract_records     — Contratos de dados analíticos com field definitions jsonb
///   ctr_deprecation_schedules     — Agendamentos de deprecação de contratos com guia de migração
/// </summary>
public partial class AO_AQ_AV_AddSbomDataContractDeprecationSchedule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ctr_sbom_records (
                ""Id""          uuid            NOT NULL PRIMARY KEY,
                ""TenantId""    varchar(200)    NOT NULL,
                ""ServiceId""   varchar(200)    NOT NULL,
                ""ServiceName"" varchar(300)    NOT NULL,
                ""Version""     varchar(100)    NOT NULL,
                ""RecordedAt""  timestamptz     NOT NULL,
                ""Components""  jsonb
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_ctr_sbom_tenant
                ON ctr_sbom_records (""TenantId"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_ctr_sbom_tenant_service
                ON ctr_sbom_records (""TenantId"", ""ServiceId"");
        ");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ctr_data_contract_records (
                ""Id""                          uuid            NOT NULL PRIMARY KEY,
                ""TenantId""                    varchar(200)    NOT NULL,
                ""ServiceId""                   varchar(200)    NOT NULL,
                ""DatasetName""                 varchar(300)    NOT NULL,
                ""ContractVersion""             varchar(100)    NOT NULL,
                ""FreshnessRequirementHours""   integer,
                ""FieldDefinitionsJson""        jsonb,
                ""OwnerTeamId""                 varchar(200),
                ""Status""                      varchar(50)     NOT NULL DEFAULT 'Active',
                ""CreatedAt""                   timestamptz     NOT NULL,
                ""UpdatedAt""                   timestamptz     NOT NULL
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_ctr_dcr_tenant
                ON ctr_data_contract_records (""TenantId"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_ctr_dcr_tenant_service
                ON ctr_data_contract_records (""TenantId"", ""ServiceId"");
        ");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ctr_deprecation_schedules (
                ""Id""                          uuid            NOT NULL PRIMARY KEY,
                ""ContractId""                  uuid            NOT NULL,
                ""TenantId""                    varchar(200)    NOT NULL,
                ""PlannedDeprecationDate""       timestamptz     NOT NULL,
                ""PlannedSunsetDate""            timestamptz,
                ""MigrationGuideUrl""            varchar(1000),
                ""SuccessorVersionId""           uuid,
                ""NotificationDraftMessage""     varchar(4000),
                ""ScheduledByUserId""            varchar(200)    NOT NULL,
                ""Reason""                       varchar(1000),
                ""ScheduledAt""                  timestamptz     NOT NULL
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_ctr_dep_schedule_contract
                ON ctr_deprecation_schedules (""ContractId"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_ctr_dep_schedule_tenant
                ON ctr_deprecation_schedules (""TenantId"");
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS ctr_deprecation_schedules;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS ctr_data_contract_records;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS ctr_sbom_records;");
    }
}
