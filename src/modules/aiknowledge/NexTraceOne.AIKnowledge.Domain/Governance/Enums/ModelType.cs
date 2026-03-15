namespace NexTraceOne.AiGovernance.Domain.Enums;

/// <summary>
/// Tipo funcional do modelo de IA registrado no Model Registry.
/// Determina em quais contextos o modelo pode ser utilizado.
/// </summary>
public enum ModelType
{
    /// <summary>Modelo conversacional (ex: GPT-4, Claude).</summary>
    Chat,

    /// <summary>Modelo de completação de texto.</summary>
    Completion,

    /// <summary>Modelo de geração de embeddings vetoriais.</summary>
    Embedding,

    /// <summary>Modelo especializado em geração de código.</summary>
    CodeGeneration,

    /// <summary>Modelo especializado em análise e raciocínio.</summary>
    Analysis
}
