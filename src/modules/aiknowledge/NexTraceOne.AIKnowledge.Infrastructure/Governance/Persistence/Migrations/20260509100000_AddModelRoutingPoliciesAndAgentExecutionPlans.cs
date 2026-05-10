using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// Adiciona tabelas persistentes para políticas de roteamento de modelo e planos de execução agentic.
///
/// Substitui as implementações em memória (NullModelRoutingPolicyRepository e NullAgentExecutionPlanRepository)
/// por repositórios EF Core com persistência real em PostgreSQL.
///
/// Tabelas:
///   1. aik_model_routing_policies — política de roteamento por intenção de prompt por tenant.
///   2. aik_agent_execution_plans  — plano de execução agentic com steps JSONB e suporte HITL.
/// </summary>
public partial class AddModelRoutingPoliciesAndAgentExecutionPlans : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── 1. aik_model_routing_policies ────────────────────────────────────
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_model_routing_policies (
                ""Id""                    uuid            NOT NULL PRIMARY KEY,
                ""TenantId""              uuid            NOT NULL,
                ""Intent""                varchar(50)     NOT NULL,
                ""PreferredModelName""    varchar(300)    NOT NULL,
                ""FallbackModelName""     varchar(300),
                ""MaxTokens""             integer         NOT NULL,
                ""MaxCostPerRequestUsd""  numeric(10,6)   NOT NULL,
                ""IsActive""              boolean         NOT NULL DEFAULT true,
                ""IsDeleted""             boolean         NOT NULL DEFAULT false,
                ""CreatedAt""             timestamptz,
                ""CreatedBy""             varchar(500),
                ""UpdatedAt""             timestamptz,
                ""UpdatedBy""             varchar(500)
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_mrp_tenant_intent_active
                ON aik_model_routing_policies (""TenantId"", ""Intent"", ""IsActive"");
        ");

        // ── 2. aik_agent_execution_plans ──────────────────────────────────────
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_agent_execution_plans (
                ""Id""                    uuid            NOT NULL PRIMARY KEY,
                ""TenantId""              uuid            NOT NULL,
                ""RequestedBy""           varchar(500)    NOT NULL,
                ""Description""           varchar(2000)   NOT NULL,
                ""PlanStatus""            varchar(50)     NOT NULL,
                ""steps""                 jsonb           NOT NULL DEFAULT '[]'::jsonb,
                ""MaxTokenBudget""        integer         NOT NULL,
                ""ConsumedTokens""        integer         NOT NULL DEFAULT 0,
                ""RequiresApproval""      boolean         NOT NULL DEFAULT false,
                ""BlastRadiusThreshold""  integer         NOT NULL DEFAULT 0,
                ""ApprovedBy""            varchar(500),
                ""ApprovedAt""            timestamptz,
                ""CorrelationId""         varchar(100)    NOT NULL,
                ""StartedAt""             timestamptz,
                ""CompletedAt""           timestamptz,
                ""ErrorMessage""          varchar(4000),
                ""IsDeleted""             boolean         NOT NULL DEFAULT false,
                ""CreatedAt""             timestamptz,
                ""CreatedBy""             varchar(500),
                ""UpdatedAt""             timestamptz,
                ""UpdatedBy""             varchar(500)
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_aep_tenant
                ON aik_agent_execution_plans (""TenantId"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_aep_correlation
                ON aik_agent_execution_plans (""CorrelationId"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_aep_status
                ON aik_agent_execution_plans (""PlanStatus"");
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_agent_execution_plans;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_model_routing_policies;");
    }
}
