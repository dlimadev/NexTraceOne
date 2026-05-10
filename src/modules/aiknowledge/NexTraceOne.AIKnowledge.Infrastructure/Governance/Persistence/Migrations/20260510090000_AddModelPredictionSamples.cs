using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// Wave AT.1 — AI Model Quality &amp; Drift Governance.
///
/// Cria a tabela aik_model_prediction_samples para persistência de amostras de predição
/// de modelos de IA em produção. Substitui o NullModelPredictionRepository que descartava
/// silenciosamente todos os dados ingeridos via IngestModelPredictionSample.
///
/// Tabela:
///   aik_model_prediction_samples — amostras de predição com estatísticas de input (jsonb),
///   classe predita, score de confiança, latência de inferência e feedback loop (ActualClass).
/// </summary>
public partial class AddModelPredictionSamples : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_model_prediction_samples (
                ""Id""                      uuid            NOT NULL PRIMARY KEY,
                ""ModelId""                 uuid            NOT NULL,
                ""ModelName""               varchar(200)    NOT NULL,
                ""ServiceId""               varchar(200)    NOT NULL,
                ""TenantId""                varchar(200)    NOT NULL,
                ""PredictedAt""             timestamptz     NOT NULL,
                ""InputFeatureStatsJson""   jsonb,
                ""PredictedClass""          varchar(200),
                ""ConfidenceScore""         double precision,
                ""InferenceLatencyMs""      integer,
                ""ActualClass""             varchar(200),
                ""IsFallback""              boolean         NOT NULL DEFAULT false,
                ""DriftAcknowledged""       boolean         NOT NULL DEFAULT false,
                ""IsDeleted""               boolean         NOT NULL DEFAULT false,
                ""CreatedAt""               timestamptz,
                ""CreatedBy""               varchar(500),
                ""UpdatedAt""               timestamptz,
                ""UpdatedBy""               varchar(500)
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_mps_tenant_predicted
                ON aik_model_prediction_samples (""TenantId"", ""PredictedAt"" DESC);
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_mps_model_tenant
                ON aik_model_prediction_samples (""ModelId"", ""TenantId"");
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_model_prediction_samples;");
    }
}
