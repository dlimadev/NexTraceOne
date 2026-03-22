using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// Padroniza TenantId de string para uuid em AiExternalInferenceRecords e AiTokenUsageLedger.
/// Alinha com o padrão global da plataforma onde TenantId é sempre Guid.
///
/// IMPACTO:
/// - Dados existentes com TenantId string serão convertidos para uuid.
/// - Registros com TenantId vazio ou inválido receberão Guid.Empty ('00000000-0000-0000-0000-000000000000').
/// - Índices são recriados com o novo tipo.
/// </summary>
public partial class StandardizeTenantIdToGuid : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // AiExternalInferenceRecords: converter coluna TenantId de varchar(200) para uuid
        migrationBuilder.Sql("""
            ALTER TABLE "AiExternalInferenceRecords"
            ALTER COLUMN "TenantId" TYPE uuid
            USING CASE
                WHEN "TenantId" ~ '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$'
                THEN "TenantId"::uuid
                ELSE '00000000-0000-0000-0000-000000000000'::uuid
            END;
            """);

        // AiTokenUsageLedger: converter coluna TenantId de varchar(200) para uuid
        migrationBuilder.Sql("""
            ALTER TABLE "AiTokenUsageLedger"
            ALTER COLUMN "TenantId" TYPE uuid
            USING CASE
                WHEN "TenantId" ~ '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$'
                THEN "TenantId"::uuid
                ELSE '00000000-0000-0000-0000-000000000000'::uuid
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverter uuid para varchar(200)
        migrationBuilder.Sql("""
            ALTER TABLE "AiExternalInferenceRecords"
            ALTER COLUMN "TenantId" TYPE character varying(200)
            USING "TenantId"::text;
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "AiTokenUsageLedger"
            ALTER COLUMN "TenantId" TYPE character varying(200)
            USING "TenantId"::text;
            """);
    }
}
