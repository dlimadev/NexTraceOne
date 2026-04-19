using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// Phase 9 — AI Skills System.
///
/// Esta migração cria as tabelas do sistema de skills de IA:
///   1. aik_skills — catálogo de skills com ownership, visibilidade e conteúdo SKILL.md.
///   2. aik_skill_executions — log de execuções de skills com métricas de tokens.
///   3. aik_skill_feedbacks — feedback de utilizadores para ciclo RL.
///
/// Índices criados:
///   - idx_aik_skills_name_tenant — pesquisa por nome dentro de um tenant.
///   - idx_aik_skills_ownership_type — filtro por tipo de ownership.
///   - idx_aik_skills_status — filtro por estado.
///   - idx_aik_skills_tenant_id — filtro por tenant.
///   - idx_aik_skill_executions_skill_id — execuções por skill.
///   - idx_aik_skill_executions_executed_by — execuções por utilizador.
///   - idx_aik_skill_executions_executed_at — ordenação temporal.
///   - idx_aik_skill_feedbacks_execution_id — feedbacks por execução.
///   - idx_aik_skill_feedbacks_tenant_id — feedbacks por tenant.
/// </summary>
public partial class AddSkillsSystem : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── 1. aik_skills ─────────────────────────────────────────────────────
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_skills (
                ""Id""               uuid         NOT NULL PRIMARY KEY,
                ""Name""             varchar(200) NOT NULL,
                ""DisplayName""      varchar(300) NOT NULL,
                ""Description""      varchar(2000),
                ""SkillContent""     text,
                ""Version""          varchar(50)  NOT NULL DEFAULT '1.0.0',
                ""OwnershipType""    varchar(50)  NOT NULL,
                ""Visibility""       varchar(50)  NOT NULL,
                ""Status""           varchar(50)  NOT NULL,
                ""Tags""             varchar(2000),
                ""RequiredTools""    varchar(2000),
                ""PreferredModels""  varchar(2000),
                ""InputSchema""      varchar(16000),
                ""OutputSchema""     varchar(16000),
                ""ExecutionCount""   bigint       NOT NULL DEFAULT 0,
                ""AverageRating""    double precision NOT NULL DEFAULT 0,
                ""ParentAgentId""    varchar(200),
                ""IsComposable""     boolean      NOT NULL DEFAULT false,
                ""OwnerId""          varchar(200),
                ""OwnerTeamId""      varchar(200),
                ""TenantId""         uuid         NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                ""xmin""             xid          NOT NULL,
                ""CreatedAt""        timestamptz,
                ""CreatedBy""        varchar(500),
                ""UpdatedAt""        timestamptz,
                ""UpdatedBy""        varchar(500)
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_skills_name_tenant
                ON aik_skills (""Name"", ""TenantId"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_skills_ownership_type
                ON aik_skills (""OwnershipType"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_skills_status
                ON aik_skills (""Status"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_skills_tenant_id
                ON aik_skills (""TenantId"");
        ");

        // ── 2. aik_skill_executions ───────────────────────────────────────────
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_skill_executions (
                ""Id""               uuid         NOT NULL PRIMARY KEY,
                ""SkillId""          uuid         NOT NULL,
                ""AgentId""          uuid,
                ""ExecutedBy""       varchar(200) NOT NULL,
                ""ModelUsed""        varchar(200),
                ""InputJson""        varchar(32000),
                ""OutputJson""       varchar(64000),
                ""DurationMs""       bigint       NOT NULL DEFAULT 0,
                ""PromptTokens""     integer      NOT NULL DEFAULT 0,
                ""CompletionTokens"" integer      NOT NULL DEFAULT 0,
                ""TotalTokens""      integer      NOT NULL DEFAULT 0,
                ""IsSuccess""        boolean      NOT NULL DEFAULT true,
                ""ErrorMessage""     varchar(4000),
                ""TenantId""         uuid         NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                ""ExecutedAt""       timestamptz  NOT NULL,
                ""CreatedAt""        timestamptz,
                ""CreatedBy""        varchar(500),
                ""UpdatedAt""        timestamptz,
                ""UpdatedBy""        varchar(500)
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_skill_executions_skill_id
                ON aik_skill_executions (""SkillId"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_skill_executions_executed_by
                ON aik_skill_executions (""ExecutedBy"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_skill_executions_executed_at
                ON aik_skill_executions (""ExecutedAt"");
        ");

        // ── 3. aik_skill_feedbacks ────────────────────────────────────────────
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_skill_feedbacks (
                ""Id""               uuid         NOT NULL PRIMARY KEY,
                ""SkillExecutionId"" uuid         NOT NULL,
                ""Rating""           integer      NOT NULL,
                ""Outcome""          varchar(100) NOT NULL,
                ""Comment""          varchar(2000),
                ""ActualOutcome""    varchar(2000),
                ""WasCorrect""       boolean      NOT NULL DEFAULT false,
                ""SubmittedBy""      varchar(200) NOT NULL,
                ""TenantId""         uuid         NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                ""SubmittedAt""      timestamptz  NOT NULL,
                ""CreatedAt""        timestamptz,
                ""CreatedBy""        varchar(500),
                ""UpdatedAt""        timestamptz,
                ""UpdatedBy""        varchar(500)
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_skill_feedbacks_execution_id
                ON aik_skill_feedbacks (""SkillExecutionId"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_skill_feedbacks_tenant_id
                ON aik_skill_feedbacks (""TenantId"");
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_skill_feedbacks;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_skill_executions;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_skills;");
    }
}
