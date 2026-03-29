using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Catálogo estático de modelos de IA padrão da plataforma.
/// Utilizado pela feature SeedDefaultModels para garantir que o Model Registry
/// contenha os modelos mínimos necessários para operação imediata.
///
/// Cada entrada define um modelo com as suas propriedades fundamentais:
/// nome técnico, provedor, tipo funcional, capacidades e nível de sensibilidade.
///
/// O catálogo é determinístico e idempotente — não cria modelos duplicados.
/// </summary>
public static class DefaultModelCatalog
{
    /// <summary>Definição de um modelo padrão para seed.</summary>
    public sealed record ModelDefinition(
        string Name,
        string DisplayName,
        string Provider,
        ModelType ModelType,
        bool IsInternal,
        string Capabilities,
        int SensitivityLevel,
        string Category,
        bool IsDefaultForChat,
        bool IsDefaultForReasoning,
        bool IsDefaultForEmbeddings,
        bool SupportsStreaming,
        bool SupportsToolCalling,
        bool SupportsEmbeddings,
        bool SupportsVision,
        bool SupportsStructuredOutput,
        int? ContextWindow,
        bool RequiresGpu,
        string LicenseName);

    /// <summary>
    /// Retorna a lista completa de modelos padrão da plataforma.
    /// Inclui modelos internos (Ollama/local) e modelos externos (OpenAI, Anthropic).
    /// </summary>
    public static IReadOnlyList<ModelDefinition> GetAll() => Models;

    private static readonly IReadOnlyList<ModelDefinition> Models = new[]
    {
        // ── Internal / Local models (Ollama) ────────────────────────────
        new ModelDefinition(
            Name: "deepseek-r1:1.5b",
            DisplayName: "DeepSeek R1 1.5B",
            Provider: "Ollama",
            ModelType: ModelType.Chat,
            IsInternal: true,
            Capabilities: "chat,reasoning",
            SensitivityLevel: 1,
            Category: "reasoning",
            IsDefaultForChat: false,
            IsDefaultForReasoning: true,
            IsDefaultForEmbeddings: false,
            SupportsStreaming: true,
            SupportsToolCalling: false,
            SupportsEmbeddings: false,
            SupportsVision: false,
            SupportsStructuredOutput: true,
            ContextWindow: 32768,
            RequiresGpu: false,
            LicenseName: "MIT"),

        new ModelDefinition(
            Name: "llama3.2:3b",
            DisplayName: "Llama 3.2 3B",
            Provider: "Ollama",
            ModelType: ModelType.Chat,
            IsInternal: true,
            Capabilities: "chat,code,general",
            SensitivityLevel: 1,
            Category: "general",
            IsDefaultForChat: true,
            IsDefaultForReasoning: false,
            IsDefaultForEmbeddings: false,
            SupportsStreaming: true,
            SupportsToolCalling: true,
            SupportsEmbeddings: false,
            SupportsVision: false,
            SupportsStructuredOutput: true,
            ContextWindow: 131072,
            RequiresGpu: false,
            LicenseName: "Llama 3.2 Community License"),

        new ModelDefinition(
            Name: "nomic-embed-text",
            DisplayName: "Nomic Embed Text",
            Provider: "Ollama",
            ModelType: ModelType.Embedding,
            IsInternal: true,
            Capabilities: "embeddings",
            SensitivityLevel: 1,
            Category: "embeddings",
            IsDefaultForChat: false,
            IsDefaultForReasoning: false,
            IsDefaultForEmbeddings: true,
            SupportsStreaming: false,
            SupportsToolCalling: false,
            SupportsEmbeddings: true,
            SupportsVision: false,
            SupportsStructuredOutput: false,
            ContextWindow: 8192,
            RequiresGpu: false,
            LicenseName: "Apache 2.0"),

        new ModelDefinition(
            Name: "codellama:7b",
            DisplayName: "Code Llama 7B",
            Provider: "Ollama",
            ModelType: ModelType.CodeGeneration,
            IsInternal: true,
            Capabilities: "code,completion",
            SensitivityLevel: 1,
            Category: "code",
            IsDefaultForChat: false,
            IsDefaultForReasoning: false,
            IsDefaultForEmbeddings: false,
            SupportsStreaming: true,
            SupportsToolCalling: false,
            SupportsEmbeddings: false,
            SupportsVision: false,
            SupportsStructuredOutput: false,
            ContextWindow: 16384,
            RequiresGpu: true,
            LicenseName: "Llama 2 Community License"),

        // ── External models (OpenAI) ────────────────────────────────────
        new ModelDefinition(
            Name: "gpt-4o",
            DisplayName: "GPT-4o",
            Provider: "OpenAI",
            ModelType: ModelType.Chat,
            IsInternal: false,
            Capabilities: "chat,code,reasoning,vision",
            SensitivityLevel: 3,
            Category: "general",
            IsDefaultForChat: false,
            IsDefaultForReasoning: false,
            IsDefaultForEmbeddings: false,
            SupportsStreaming: true,
            SupportsToolCalling: true,
            SupportsEmbeddings: false,
            SupportsVision: true,
            SupportsStructuredOutput: true,
            ContextWindow: 128000,
            RequiresGpu: false,
            LicenseName: "Proprietary"),

        new ModelDefinition(
            Name: "gpt-4o-mini",
            DisplayName: "GPT-4o Mini",
            Provider: "OpenAI",
            ModelType: ModelType.Chat,
            IsInternal: false,
            Capabilities: "chat,code",
            SensitivityLevel: 3,
            Category: "general",
            IsDefaultForChat: false,
            IsDefaultForReasoning: false,
            IsDefaultForEmbeddings: false,
            SupportsStreaming: true,
            SupportsToolCalling: true,
            SupportsEmbeddings: false,
            SupportsVision: true,
            SupportsStructuredOutput: true,
            ContextWindow: 128000,
            RequiresGpu: false,
            LicenseName: "Proprietary"),

        // ── External models (Anthropic) ─────────────────────────────────
        new ModelDefinition(
            Name: "claude-3-5-sonnet",
            DisplayName: "Claude 3.5 Sonnet",
            Provider: "Anthropic",
            ModelType: ModelType.Chat,
            IsInternal: false,
            Capabilities: "chat,code,reasoning,analysis",
            SensitivityLevel: 3,
            Category: "general",
            IsDefaultForChat: false,
            IsDefaultForReasoning: false,
            IsDefaultForEmbeddings: false,
            SupportsStreaming: true,
            SupportsToolCalling: true,
            SupportsEmbeddings: false,
            SupportsVision: true,
            SupportsStructuredOutput: true,
            ContextWindow: 200000,
            RequiresGpu: false,
            LicenseName: "Proprietary"),
    };
}
