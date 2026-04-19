using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// Phase 10 — Agent Lightning (Reinforcement Learning).
///
/// Esta migração cria as tabelas necessárias para o sistema Agent Lightning:
///   1. aik_agent_trajectory_feedbacks — feedback de trajectória por execução de agent,
///      incluindo controlo de exportação para o trainer RL externo.
///   2. aik_agent_performance_metrics — métricas de performance por agent num período de 30 dias,
///      incluindo accuracy rate, ciclos RL completados e trajectórias exportadas.
///
/// Índices criados:
///   - idx_aik_agent_traj_fb_execution_id — lookup por execução.
///   - idx_aik_agent_traj_fb_tenant_id — filtro por tenant.
///   - idx_aik_agent_traj_fb_exported — filtro para exportação pendente.
///   - idx_aik_agent_perf_agent_id — métricas por agent.
///   - idx_aik_agent_perf_tenant_id — métricas por tenant.
///   - idx_aik_agent_perf_period_start — filtro por período.
/// </summary>
public partial class AddAgentLightning : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── 1. aik_agent_trajectory_feedbacks ────────────────────────────────
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_agent_trajectory_feedbacks (
                ""Id""                    uuid         NOT NULL PRIMARY KEY,
                ""ExecutionId""           uuid         NOT NULL,
                ""Rating""                integer      NOT NULL,
                ""Outcome""               varchar(100) NOT NULL,
                ""Comment""               varchar(2000),
                ""ActualOutcome""         varchar(2000),
                ""WasCorrect""            boolean      NOT NULL DEFAULT false,
                ""TimeToResolveMinutes""  integer,
                ""SubmittedBy""           varchar(200) NOT NULL,
                ""TenantId""              uuid         NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                ""SubmittedAt""           timestamptz  NOT NULL,
                ""ExportedForTraining""   boolean      NOT NULL DEFAULT false,
                ""ExportedAt""            timestamptz,
                ""CreatedAt""             timestamptz,
                ""CreatedBy""             varchar(500),
                ""UpdatedAt""             timestamptz,
                ""UpdatedBy""             varchar(500)
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_agent_traj_fb_execution_id
                ON aik_agent_trajectory_feedbacks (""ExecutionId"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_agent_traj_fb_tenant_id
                ON aik_agent_trajectory_feedbacks (""TenantId"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_agent_traj_fb_exported
                ON aik_agent_trajectory_feedbacks (""ExportedForTraining"");
        ");

        // ── 2. aik_agent_performance_metrics ─────────────────────────────────
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_agent_performance_metrics (
                ""Id""                      uuid             NOT NULL PRIMARY KEY,
                ""AgentId""                 uuid             NOT NULL,
                ""AgentName""               varchar(300)     NOT NULL,
                ""PeriodStart""             timestamptz      NOT NULL,
                ""PeriodEnd""               timestamptz      NOT NULL,
                ""TotalExecutions""         bigint           NOT NULL DEFAULT 0,
                ""ExecutionsWithFeedback""  bigint           NOT NULL DEFAULT 0,
                ""AverageRating""           double precision NOT NULL DEFAULT 0,
                ""AccuracyRate""            double precision NOT NULL DEFAULT 0,
                ""RlCyclesCompleted""       integer          NOT NULL DEFAULT 0,
                ""TrajectoriesExported""    bigint           NOT NULL DEFAULT 0,
                ""TenantId""                uuid             NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                ""CreatedAt""               timestamptz,
                ""CreatedBy""               varchar(500),
                ""UpdatedAt""               timestamptz,
                ""UpdatedBy""               varchar(500)
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_agent_perf_agent_id
                ON aik_agent_performance_metrics (""AgentId"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_agent_perf_tenant_id
                ON aik_agent_performance_metrics (""TenantId"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_agent_perf_period_start
                ON aik_agent_performance_metrics (""PeriodStart"");
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_agent_performance_metrics;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_agent_trajectory_feedbacks;");
    }
}
