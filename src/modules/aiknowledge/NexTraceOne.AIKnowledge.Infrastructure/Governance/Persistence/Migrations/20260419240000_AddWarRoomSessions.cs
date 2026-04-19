using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// Fase 11 — War Room Sessions para coordenação de incidentes P0/P1.
/// Cria tabela aik_war_rooms.
/// </summary>
public partial class AddWarRoomSessions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_war_rooms (
                ""Id""                    uuid         NOT NULL PRIMARY KEY,
                ""IncidentId""            varchar(100) NOT NULL,
                ""IncidentTitle""         varchar(500) NOT NULL,
                ""Severity""              varchar(10)  NOT NULL,
                ""Status""                varchar(50)  NOT NULL DEFAULT 'Open',
                ""ServiceAffected""       varchar(300),
                ""CreatedByAgentId""      varchar(200),
                ""ParticipantsJson""      text         NOT NULL DEFAULT '[]',
                ""TimelineJson""          text         NOT NULL DEFAULT '[]',
                ""SuggestedActionsJson""  text         NOT NULL DEFAULT '[]',
                ""PostMortemDraft""       text,
                ""TenantId""              uuid         NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                ""OpenedAt""              timestamptz  NOT NULL,
                ""ResolvedAt""            timestamptz,
                ""SkillUsed""             varchar(200),
                ""CreatedAt""             timestamptz,
                ""CreatedBy""             varchar(500),
                ""UpdatedAt""             timestamptz,
                ""UpdatedBy""             varchar(500)
            );
        ");

        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_war_rooms_tenant_id ON aik_war_rooms (""TenantId"");");
        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_war_rooms_incident_id ON aik_war_rooms (""IncidentId"");");
        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_war_rooms_status ON aik_war_rooms (""Status"");");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_war_rooms;");
    }
}
