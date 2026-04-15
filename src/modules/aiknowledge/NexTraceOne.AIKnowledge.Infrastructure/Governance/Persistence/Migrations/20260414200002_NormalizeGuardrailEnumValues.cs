using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// E-M01: Guardrails com enums fortemente tipados.
/// - Normaliza os valores existentes das colunas Category, GuardType, PatternType, Severity, Action
///   para corresponder ao formato PascalCase dos novos enums.
/// - Não altera o tipo das colunas (permanecem TEXT) — EF Core usa HasConversion{string}
///   que serializa o enum como o seu nome.
/// </summary>
public partial class NormalizeGuardrailEnumValues : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Normalizar Category: security → Security, privacy → Privacy, etc.
        migrationBuilder.Sql(@"
            UPDATE aik_guardrails SET ""Category"" = CASE LOWER(""Category"")
                WHEN 'security'   THEN 'Security'
                WHEN 'privacy'    THEN 'Privacy'
                WHEN 'compliance' THEN 'Compliance'
                WHEN 'quality'    THEN 'Quality'
                ELSE INITCAP(""Category"")
            END
            WHERE ""Category"" IS NOT NULL AND ""Category"" != '';
        ");

        // Normalizar GuardType: input → Input, output → Output, both → Both
        migrationBuilder.Sql(@"
            UPDATE aik_guardrails SET ""GuardType"" = CASE LOWER(""GuardType"")
                WHEN 'input'  THEN 'Input'
                WHEN 'output' THEN 'Output'
                WHEN 'both'   THEN 'Both'
                ELSE INITCAP(""GuardType"")
            END
            WHERE ""GuardType"" IS NOT NULL AND ""GuardType"" != '';
        ");

        // Normalizar PatternType: regex → Regex, keyword → Keyword, classifier → Classifier, semantic → Semantic
        migrationBuilder.Sql(@"
            UPDATE aik_guardrails SET ""PatternType"" = CASE LOWER(""PatternType"")
                WHEN 'regex'      THEN 'Regex'
                WHEN 'keyword'    THEN 'Keyword'
                WHEN 'classifier' THEN 'Classifier'
                WHEN 'semantic'   THEN 'Semantic'
                ELSE INITCAP(""PatternType"")
            END
            WHERE ""PatternType"" IS NOT NULL AND ""PatternType"" != '';
        ");

        // Normalizar Severity: critical → Critical, high → High, medium → Medium, low → Low, info → Info
        migrationBuilder.Sql(@"
            UPDATE aik_guardrails SET ""Severity"" = CASE LOWER(""Severity"")
                WHEN 'critical' THEN 'Critical'
                WHEN 'high'     THEN 'High'
                WHEN 'medium'   THEN 'Medium'
                WHEN 'low'      THEN 'Low'
                WHEN 'info'     THEN 'Info'
                ELSE INITCAP(""Severity"")
            END
            WHERE ""Severity"" IS NOT NULL AND ""Severity"" != '';
        ");

        // Normalizar Action: block → Block, sanitize → Sanitize, warn → Warn, log → Log
        migrationBuilder.Sql(@"
            UPDATE aik_guardrails SET ""Action"" = CASE LOWER(""Action"")
                WHEN 'block'    THEN 'Block'
                WHEN 'sanitize' THEN 'Sanitize'
                WHEN 'warn'     THEN 'Warn'
                WHEN 'log'      THEN 'Log'
                ELSE INITCAP(""Action"")
            END
            WHERE ""Action"" IS NOT NULL AND ""Action"" != '';
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reversão: PascalCase → lowercase (best-effort)
        migrationBuilder.Sql(@"
            UPDATE aik_guardrails SET ""Category""    = LOWER(""Category""),
                                      ""GuardType""   = LOWER(""GuardType""),
                                      ""PatternType"" = LOWER(""PatternType""),
                                      ""Severity""    = LOWER(""Severity""),
                                      ""Action""      = LOWER(""Action"");
        ");
    }
}
