using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Domain.Orchestration.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo AiOrchestration com códigos i18n.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: AiOrchestration.{Entidade}.{Descrição}
/// </summary>
public static class AiOrchestrationErrors
{
    /// <summary>Conversa de IA não encontrada pelo identificador informado.</summary>
    public static Error ConversationNotFound(string conversationId)
        => Error.NotFound(
            "AiOrchestration.Conversation.NotFound",
            "AI conversation '{0}' was not found.",
            conversationId);

    /// <summary>Conversa de IA não está ativa — não aceita novos turnos.</summary>
    public static Error ConversationNotActive(string conversationId)
        => Error.Business(
            "AiOrchestration.Conversation.NotActive",
            "AI conversation '{0}' is not active.",
            conversationId);

    /// <summary>Conversa de IA já foi concluída ou expirou — não aceita nova transição.</summary>
    public static Error ConversationAlreadyCompleted(string conversationId)
        => Error.Conflict(
            "AiOrchestration.Conversation.AlreadyCompleted",
            "AI conversation '{0}' has already been completed or expired.",
            conversationId);

    /// <summary>Contexto de IA não encontrado pelo identificador informado.</summary>
    public static Error ContextNotFound(string contextId)
        => Error.NotFound(
            "AiOrchestration.Context.NotFound",
            "AI context '{0}' was not found.",
            contextId);

    /// <summary>Artefato de teste gerado não encontrado pelo identificador informado.</summary>
    public static Error ArtifactNotFound(string artifactId)
        => Error.NotFound(
            "AiOrchestration.Artifact.NotFound",
            "Generated test artifact '{0}' was not found.",
            artifactId);

    /// <summary>Artefato de teste já foi revisado — não aceita nova revisão.</summary>
    public static Error ArtifactAlreadyReviewed(string artifactId)
        => Error.Conflict(
            "AiOrchestration.Artifact.AlreadyReviewed",
            "Generated test artifact '{0}' has already been reviewed.",
            artifactId);

    /// <summary>Entrada de conhecimento não encontrada pelo identificador informado.</summary>
    public static Error EntryNotFound(string entryId)
        => Error.NotFound(
            "AiOrchestration.Entry.NotFound",
            "Knowledge capture entry '{0}' was not found.",
            entryId);

    /// <summary>Entrada de conhecimento já foi processada — não aceita nova validação.</summary>
    public static Error EntryAlreadyProcessed(string entryId)
        => Error.Conflict(
            "AiOrchestration.Entry.AlreadyProcessed",
            "Knowledge capture entry '{0}' has already been processed.",
            entryId);

    /// <summary>Valor de relevância/confiança inválido — deve estar no intervalo [0, 1].</summary>
    public static Error InvalidRelevance(decimal relevance)
        => Error.Validation(
            "AiOrchestration.Entry.InvalidRelevance",
            "Relevance value ({0}) must be between 0 and 1.",
            relevance);

    /// <summary>Valor de confiança inválido — deve estar no intervalo [0, 1].</summary>
    public static Error InvalidConfidence(decimal confidence)
        => Error.Validation(
            "AiOrchestration.Artifact.InvalidConfidence",
            "Confidence value ({0}) must be between 0 and 1.",
            confidence);
}
