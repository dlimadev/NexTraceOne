using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// AI-5.2: Prompt Asset Registry — prompt gerido como contrato, versionado e comparável.
///
/// Tabelas:
///   1. aik_prompt_assets — asset identificado por slug único por tenant.
///   2. aik_prompt_versions — versões imutáveis do conteúdo do asset.
///
/// Índices:
///   - ux_aik_prompt_assets_slug_tenant — unicidade slug × tenant.
///   - idx_aik_prompt_assets_category — filtro por categoria.
///   - ux_aik_prompt_versions_asset_version — unicidade asset × número de versão.
/// </summary>
public partial class AI52_AddPromptAssets : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── 1. aik_prompt_assets ──────────────────────────────────────────────
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_prompt_assets (
                ""Id""                     uuid          NOT NULL PRIMARY KEY,
                ""Slug""                   varchar(200)  NOT NULL,
                ""Name""                   varchar(300)  NOT NULL,
                ""Description""            varchar(2000),
                ""Category""               varchar(100)  NOT NULL,
                ""Tags""                   varchar(500),
                ""Variables""              varchar(500),
                ""TenantId""               uuid,
                ""CreatedBy""              varchar(200)  NOT NULL,
                ""CurrentVersionNumber""   integer       NOT NULL DEFAULT 1,
                ""IsActive""               boolean       NOT NULL DEFAULT true,
                ""CreatedAt""              timestamptz,
                ""UpdatedAt""              timestamptz,
                ""UpdatedBy""              varchar(500)
            );
        ");

        migrationBuilder.Sql(@"
            CREATE UNIQUE INDEX IF NOT EXISTS ux_aik_prompt_assets_slug_tenant
                ON aik_prompt_assets (""Slug"", ""TenantId"");
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_aik_prompt_assets_category
                ON aik_prompt_assets (""Category"");
        ");

        // ── 2. aik_prompt_versions ────────────────────────────────────────────
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS aik_prompt_versions (
                ""Id""             uuid          NOT NULL PRIMARY KEY,
                ""AssetId""        uuid          NOT NULL REFERENCES aik_prompt_assets (""Id"") ON DELETE CASCADE,
                ""VersionNumber""  integer       NOT NULL,
                ""Content""        text          NOT NULL,
                ""ChangeNotes""    varchar(1000),
                ""EvalScore""      numeric(5,4),
                ""IsActive""       boolean       NOT NULL DEFAULT true,
                ""CreatedBy""      varchar(200)  NOT NULL,
                ""CreatedAt""      timestamptz,
                ""UpdatedAt""      timestamptz,
                ""UpdatedBy""      varchar(500)
            );
        ");

        migrationBuilder.Sql(@"
            CREATE UNIQUE INDEX IF NOT EXISTS ux_aik_prompt_versions_asset_version
                ON aik_prompt_versions (""AssetId"", ""VersionNumber"");
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_prompt_versions;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS aik_prompt_assets;");
    }
}
