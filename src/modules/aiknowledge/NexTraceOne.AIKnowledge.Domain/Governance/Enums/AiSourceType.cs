namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Tipo de fonte de dados registada para grounding e retrieval de IA.
/// Determina como a plataforma se conecta e indexa o conteúdo da fonte.
/// </summary>
public enum AiSourceType
{
    /// <summary>Documentos (Markdown, PDF, Wiki, Confluence, etc.).</summary>
    Document = 0,

    /// <summary>Base de dados relacional ou documental.</summary>
    Database = 1,

    /// <summary>Dados de telemetria (métricas, traces, logs).</summary>
    Telemetry = 2,

    /// <summary>Memória externa ou vector store partilhado.</summary>
    ExternalMemory = 3
}
