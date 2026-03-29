using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class W00_SeedLegacyFeatureFlags : Migration
    {
        /// <summary>
        /// Wave-00: Regista módulo e feature flags para capabilities legacy/mainframe.
        /// Feature flags ficam desativadas por defeito (defaultEnabled = false).
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Módulo de configuração "legacy" ──────────────────────────
            migrationBuilder.Sql("""
                INSERT INTO cfg_modules ("Id", "Key", "DisplayName", "Description", "SortOrder", "IsActive", "CreatedAt")
                VALUES (
                    'a1b2c3d4-0001-4000-8000-000000000001'::uuid,
                    'legacy',
                    'Legacy / Core Systems',
                    'Feature flags for legacy and mainframe core system capabilities.',
                    100,
                    true,
                    NOW()
                )
                ON CONFLICT ("Key") DO NOTHING;
            """);

            // ── Feature flag definitions ─────────────────────────────────
            migrationBuilder.Sql("""
                INSERT INTO cfg_feature_flag_definitions
                    ("Id", "Key", "DisplayName", "Description", "DefaultEnabled", "AllowedScopes", "ModuleId", "IsActive", "IsEditable", "CreatedAt")
                VALUES
                    (
                        'b1000000-0000-4000-8000-000000000001'::uuid,
                        'legacy.enabled',
                        'Legacy Systems',
                        'Gate principal para toda a capability legacy/mainframe. Quando desativada, todas as funcionalidades legacy ficam indisponíveis.',
                        false,
                        ARRAY['System', 'Tenant', 'Environment']::text[],
                        'a1b2c3d4-0001-4000-8000-000000000001'::uuid,
                        true, true, NOW()
                    ),
                    (
                        'b1000000-0000-4000-8000-000000000002'::uuid,
                        'legacy.batch-intelligence.enabled',
                        'Batch Intelligence',
                        'Ativa o módulo de batch intelligence — monitorização de batch jobs, SLA, baselines e regressão de duração.',
                        false,
                        ARRAY['System', 'Tenant', 'Environment']::text[],
                        'a1b2c3d4-0001-4000-8000-000000000001'::uuid,
                        true, true, NOW()
                    ),
                    (
                        'b1000000-0000-4000-8000-000000000003'::uuid,
                        'legacy.messaging-intelligence.enabled',
                        'Messaging Intelligence',
                        'Ativa o módulo de messaging intelligence — topologia MQ, queue depth, anomalias e DLQ monitoring.',
                        false,
                        ARRAY['System', 'Tenant', 'Environment']::text[],
                        'a1b2c3d4-0001-4000-8000-000000000001'::uuid,
                        true, true, NOW()
                    ),
                    (
                        'b1000000-0000-4000-8000-000000000004'::uuid,
                        'legacy.copybook-parser.enabled',
                        'Copybook Parser',
                        'Ativa o parser de copybooks COBOL — importação, versionamento e diff semântico de copybooks.',
                        false,
                        ARRAY['System', 'Tenant', 'Environment']::text[],
                        'a1b2c3d4-0001-4000-8000-000000000001'::uuid,
                        true, true, NOW()
                    ),
                    (
                        'b1000000-0000-4000-8000-000000000005'::uuid,
                        'legacy.telemetry-ingestion.enabled',
                        'Legacy Telemetry Ingestion',
                        'Ativa a ingestão de telemetria mainframe — SMF, SYSLOG, job logs, CICS statistics via OTel Collector ou API.',
                        false,
                        ARRAY['System', 'Tenant', 'Environment']::text[],
                        'a1b2c3d4-0001-4000-8000-000000000001'::uuid,
                        true, true, NOW()
                    )
                ON CONFLICT ("Key") DO NOTHING;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM cfg_feature_flag_definitions
                WHERE "Key" IN (
                    'legacy.enabled',
                    'legacy.batch-intelligence.enabled',
                    'legacy.messaging-intelligence.enabled',
                    'legacy.copybook-parser.enabled',
                    'legacy.telemetry-ingestion.enabled'
                );
            """);

            migrationBuilder.Sql("""
                DELETE FROM cfg_modules WHERE "Key" = 'legacy';
            """);
        }
    }
}
