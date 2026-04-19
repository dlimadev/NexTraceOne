using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// Fase 11 — Guardian Alerts para o Proactive Architecture Guardian.
/// Cria tabela aik_guardian_alerts.
/// </summary>
public partial class AddGuardianAlerts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_guardian_alerts (
                ""Id""               uuid             NOT NULL PRIMARY KEY,
                ""ServiceName""      varchar(300)     NOT NULL,
                ""Category""         varchar(100),
                ""PatternDetected""  varchar(2000)    NOT NULL,
                ""Recommendation""   varchar(2000),
                ""Confidence""       double precision NOT NULL DEFAULT 0,
                ""Severity""         varchar(50),
                ""Status""           varchar(50)      NOT NULL DEFAULT 'open',
                ""AcknowledgedBy""   varchar(200),
                ""AcknowledgedAt""   timestamptz,
                ""DismissReason""    varchar(1000),
                ""WasActualIssue""   boolean          NOT NULL DEFAULT false,
                ""TenantId""         uuid             NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                ""DetectedAt""       timestamptz      NOT NULL,
                ""CreatedAt""        timestamptz,
                ""CreatedBy""        varchar(500),
                ""UpdatedAt""        timestamptz,
                ""UpdatedBy""        varchar(500)
            );
        ");

        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_guardian_tenant ON aik_guardian_alerts (""TenantId"");");
        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_guardian_status ON aik_guardian_alerts (""Status"");");
        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_guardian_service ON aik_guardian_alerts (""ServiceName"", ""TenantId"");");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_guardian_alerts;");
    }
}
