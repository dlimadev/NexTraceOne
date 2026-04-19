using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// Fase 11 — Change Confidence Score para scoring de confiança pré-deployment.
/// Cria tabela aik_change_confidence_scores.
/// </summary>
public partial class AddChangeConfidenceScore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_change_confidence_scores (
                ""Id""                        uuid             NOT NULL PRIMARY KEY,
                ""ChangeId""                  varchar(200)     NOT NULL,
                ""ServiceName""               varchar(300)     NOT NULL,
                ""TenantId""                  uuid             NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                ""Score""                     integer          NOT NULL DEFAULT 0,
                ""Verdict""                   varchar(50)      NOT NULL,
                ""BlastRadiusScore""          double precision NOT NULL DEFAULT 0,
                ""TestCoverageScore""         double precision NOT NULL DEFAULT 0,
                ""IncidentHistoryScore""      double precision NOT NULL DEFAULT 0,
                ""TimeOfDayScore""            double precision NOT NULL DEFAULT 0,
                ""DeployerExperienceScore""   double precision NOT NULL DEFAULT 0,
                ""ChangeSizeScore""           double precision NOT NULL DEFAULT 0,
                ""DependencyStabilityScore""  double precision NOT NULL DEFAULT 0,
                ""ScoreBreakdownJson""        text,
                ""RecommendationText""        varchar(2000),
                ""CalculatedBy""              varchar(200)     NOT NULL,
                ""CalculatedAt""              timestamptz      NOT NULL,
                ""CreatedAt""                 timestamptz,
                ""CreatedBy""                 varchar(500),
                ""UpdatedAt""                 timestamptz,
                ""UpdatedBy""                 varchar(500)
            );
        ");

        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_ccs_change_tenant ON aik_change_confidence_scores (""ChangeId"", ""TenantId"");");
        migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS idx_aik_ccs_service_tenant ON aik_change_confidence_scores (""ServiceName"", ""TenantId"");");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_change_confidence_scores;");
    }
}
