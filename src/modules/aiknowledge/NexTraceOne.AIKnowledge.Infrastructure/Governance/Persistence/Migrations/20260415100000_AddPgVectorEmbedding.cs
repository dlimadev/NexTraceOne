using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Migrations;

/// <summary>
/// E-A01 — RAG escalável com pgvector real.
///
/// Esta migração:
///   1. Activa a extensão pgvector no cluster PostgreSQL.
///   2. Adiciona coluna 'EmbeddingVector' do tipo vector(768) à tabela aik_knowledge_sources.
///      - A dimensão 768 corresponde ao modelo nomic-embed-text (padrão do NexTraceOne).
///      - O tipo 'vector(768)' requer a extensão pgvector instalada no servidor.
///   3. Cria índice HNSW com operador de distância coseno para retrieval semântico ANN.
///      - HNSW é preferido sobre IVFFlat por não exigir pré-treino.
///      - m=16, ef_construction=64 são valores equilibrados para RAG operacional.
///
/// Pré-requisito:
///   - PostgreSQL com pgvector instalado (imagem pgvector/pgvector:pg16 no Docker).
///   - Executar scripts/db/setup-pgvector.sh (Linux) ou setup-pgvector.ps1 (Windows).
///
/// Fallback:
///   - A coluna EmbeddingJson (TEXT) é mantida para compatibilidade e fallback.
///   - DocumentRetrievalService usa pgvector quando disponível, com fallback a cosine em memória.
/// </summary>
public partial class AddPgVectorEmbedding : Migration
{
    /// <summary>Nome da tabela alvo.</summary>
    private const string TableName = "aik_knowledge_sources";

    /// <summary>Nome da nova coluna vectorial.</summary>
    private const string VectorColumnName = "\"EmbeddingVector\"";

    /// <summary>Dimensão do embedding (nomic-embed-text, text-embedding-3-small, etc.).</summary>
    private const int EmbeddingDimension = 768;

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── 1. Activar extensão pgvector ──────────────────────────────────────
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

        // ── 2. Adicionar coluna vector(768) ───────────────────────────────────
        // Usa raw SQL porque o tipo 'vector' não é reconhecido pelo EF Core sem
        // o pacote Pgvector.EntityFrameworkCore. A coluna é nullable para
        // compatibilidade retroactiva com fontes ainda não indexadas.
        migrationBuilder.Sql($@"
            ALTER TABLE {TableName}
            ADD COLUMN IF NOT EXISTS {VectorColumnName} vector({EmbeddingDimension});
        ");

        // ── 3. Criar índice HNSW para retrieval ANN eficiente ─────────────────
        // hnsw (m=16, ef_construction=64) — bom equilíbrio entre velocidade e recall.
        // vector_cosine_ops para similaridade coseno (padrão para text embeddings).
        migrationBuilder.Sql($@"
            CREATE INDEX IF NOT EXISTS idx_aik_knowledge_sources_embedding_hnsw
            ON {TableName}
            USING hnsw ({VectorColumnName} vector_cosine_ops)
            WITH (m = 16, ef_construction = 64);
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Remover índice HNSW
        migrationBuilder.Sql(
            "DROP INDEX IF EXISTS idx_aik_knowledge_sources_embedding_hnsw;");

        // Remover coluna vector
        migrationBuilder.Sql($@"
            ALTER TABLE {TableName}
            DROP COLUMN IF EXISTS {VectorColumnName};
        ");

        // Nota: não remover a extensão vector pois pode estar em uso noutras tabelas.
    }
}
