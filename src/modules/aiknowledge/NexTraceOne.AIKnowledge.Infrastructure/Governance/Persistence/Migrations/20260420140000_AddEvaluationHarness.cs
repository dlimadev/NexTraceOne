using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// ADR-009 — AI Evaluation Harness.
/// Cria as tabelas aik_evaluation_suites, aik_evaluation_cases, aik_evaluation_runs e aik_evaluation_datasets.
/// </summary>
public partial class AddEvaluationHarness : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_evaluation_suites (
                ""Id""              uuid             NOT NULL PRIMARY KEY,
                ""Name""            varchar(300)     NOT NULL,
                ""DisplayName""     varchar(300)     NOT NULL,
                ""Description""     text             NOT NULL DEFAULT '',
                ""UseCase""         varchar(200)     NOT NULL,
                ""TargetModelId""   uuid,
                ""Status""          varchar(50)      NOT NULL DEFAULT 'Draft',
                ""Version""         varchar(50)      NOT NULL,
                ""TenantId""        uuid             NOT NULL,
                ""CreatedAt""       timestamptz,
                ""CreatedBy""       varchar(500),
                ""UpdatedAt""       timestamptz,
                ""UpdatedBy""       varchar(500)
            );
        ");

        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_eval_suites_tenant ON aik_evaluation_suites (""TenantId"");");
        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_eval_suites_tenant_usecase ON aik_evaluation_suites (""TenantId"", ""UseCase"");");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_evaluation_cases (
                ""Id""                     uuid         NOT NULL PRIMARY KEY,
                ""SuiteId""                uuid         NOT NULL REFERENCES aik_evaluation_suites(""Id"") ON DELETE CASCADE,
                ""Name""                   varchar(300) NOT NULL,
                ""InputPrompt""            text         NOT NULL,
                ""GroundingContext""        text         NOT NULL DEFAULT '',
                ""ExpectedOutputPattern""  text         NOT NULL DEFAULT '',
                ""EvaluationCriteria""     varchar(500) NOT NULL DEFAULT '',
                ""IsActive""               boolean      NOT NULL DEFAULT true,
                ""CreatedAt""              timestamptz,
                ""CreatedBy""              varchar(500),
                ""UpdatedAt""              timestamptz,
                ""UpdatedBy""              varchar(500)
            );
        ");

        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_eval_cases_suite ON aik_evaluation_cases (""SuiteId"");");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_evaluation_runs (
                ""Id""               uuid             NOT NULL PRIMARY KEY,
                ""SuiteId""          uuid             NOT NULL REFERENCES aik_evaluation_suites(""Id""),
                ""ModelId""          uuid             NOT NULL,
                ""PromptVersion""    varchar(100)     NOT NULL,
                ""Status""           varchar(50)      NOT NULL DEFAULT 'Pending',
                ""StartedAt""        timestamptz,
                ""CompletedAt""      timestamptz,
                ""TotalCases""       integer          NOT NULL DEFAULT 0,
                ""PassedCases""      integer          NOT NULL DEFAULT 0,
                ""FailedCases""      integer          NOT NULL DEFAULT 0,
                ""AverageLatencyMs"" double precision NOT NULL DEFAULT 0,
                ""TotalTokenCost""   numeric(14,6)    NOT NULL DEFAULT 0,
                ""TenantId""         uuid             NOT NULL,
                ""CreatedAt""        timestamptz,
                ""CreatedBy""        varchar(500),
                ""UpdatedAt""        timestamptz,
                ""UpdatedBy""        varchar(500)
            );
        ");

        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_eval_runs_suite ON aik_evaluation_runs (""SuiteId"");");
        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_eval_runs_tenant ON aik_evaluation_runs (""TenantId"");");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_evaluation_datasets (
                ""Id""          uuid         NOT NULL PRIMARY KEY,
                ""Name""        varchar(300) NOT NULL,
                ""Description"" text         NOT NULL DEFAULT '',
                ""UseCase""     varchar(200) NOT NULL,
                ""SourceType""  varchar(50)  NOT NULL DEFAULT 'Curated',
                ""CaseCount""   integer      NOT NULL DEFAULT 0,
                ""TenantId""    uuid         NOT NULL,
                ""CreatedAt""   timestamptz,
                ""CreatedBy""   varchar(500),
                ""UpdatedAt""   timestamptz,
                ""UpdatedBy""   varchar(500)
            );
        ");

        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_eval_datasets_tenant ON aik_evaluation_datasets (""TenantId"");");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_evaluation_runs;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_evaluation_cases;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_evaluation_suites;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_evaluation_datasets;");
    }
}
