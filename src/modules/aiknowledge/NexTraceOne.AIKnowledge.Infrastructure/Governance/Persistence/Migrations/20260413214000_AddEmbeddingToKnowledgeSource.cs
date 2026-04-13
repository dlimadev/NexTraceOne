using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// Phase 9: RAG semântico com pgvector.
/// - Adiciona coluna embedding_json (TEXT nullable) à tabela aik_knowledge_sources.
///   Armazena o vetor de embedding serializado como JSON para similaridade coseno via software.
///   Evita dependência do pgvector no MVP1 mantendo compatibilidade futura.
/// - Adiciona coluna usage_planning_mode (BOOLEAN) à tabela aik_agents
///   para activar o modo de planeamento de execução (P16).
/// </summary>
public partial class AddEmbeddingToKnowledgeSource : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── Embedding JSON para knowledge sources ──────────────────────
        migrationBuilder.AddColumn<string>(
            name: "EmbeddingJson",
            table: "aik_knowledge_sources",
            type: "text",
            nullable: true);

        // ── UsePlanningMode para agents (P16) ──────────────────────────
        migrationBuilder.AddColumn<bool>(
            name: "UsePlanningMode",
            table: "aik_agents",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "EmbeddingJson",
            table: "aik_knowledge_sources");

        migrationBuilder.DropColumn(
            name: "UsePlanningMode",
            table: "aik_agents");
    }
}
