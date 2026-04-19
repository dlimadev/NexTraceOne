using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// Fase 11 — Self-Healing Actions para auto-remediação governada por AI.
/// Cria tabela aik_self_healing_actions.
/// </summary>
public partial class AddSelfHealingActions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_self_healing_actions (
                ""Id""                  uuid             NOT NULL PRIMARY KEY,
                ""IncidentId""          varchar(200)     NOT NULL,
                ""ServiceName""         varchar(300)     NOT NULL,
                ""ActionType""          varchar(50)      NOT NULL,
                ""ActionDescription""   varchar(2000)    NOT NULL,
                ""Status""              varchar(50)      NOT NULL DEFAULT 'pending',
                ""Confidence""          double precision NOT NULL DEFAULT 0,
                ""RiskLevel""           varchar(50)      NOT NULL DEFAULT '',
                ""ApprovedBy""          varchar(200),
                ""ApprovedAt""          timestamptz,
                ""ExecutedAt""          timestamptz,
                ""Result""              varchar(2000),
                ""AuditTrailJson""      text             NOT NULL DEFAULT '[]',
                ""TenantId""            uuid             NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                ""ProposedAt""          timestamptz      NOT NULL,
                ""CreatedAt""           timestamptz,
                ""CreatedBy""           varchar(500),
                ""UpdatedAt""           timestamptz,
                ""UpdatedBy""           varchar(500),
                xmin                    xid              NOT NULL
            );
        ");

        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_sha_tenant ON aik_self_healing_actions (""TenantId"");");
        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_sha_incident ON aik_self_healing_actions (""IncidentId"", ""TenantId"");");
        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_sha_status ON aik_self_healing_actions (""Status"");");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_self_healing_actions;");
    }
}
