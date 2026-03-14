using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.AiOrchestration.Domain.Enums;
using NexTraceOne.AiOrchestration.Domain.Errors;

namespace NexTraceOne.AiOrchestration.Domain.Entities;

/// <summary>
/// Representa uma entrada sugerida para a base de conhecimento organizacional,
/// originada da orquestração de IA durante análise de mudanças ou diagnóstico de erros.
/// Cada entrada passa por validação humana antes de ser incorporada à base de conhecimento.
///
/// Ciclo de vida: Suggested → (Validated | Discarded).
///
/// Invariantes:
/// - Relevância deve estar no intervalo [0, 1].
/// - Cada entrada está vinculada a uma conversa de IA específica.
/// - Validação e descarte requerem identificação do responsável.
/// - Uma vez processada, não pode ser reprocessada.
/// </summary>
public sealed class KnowledgeCaptureEntry : AuditableEntity<KnowledgeCaptureEntryId>
{
    private KnowledgeCaptureEntry() { }

    /// <summary>Identificador da conversa de IA que originou esta sugestão.</summary>
    public AiConversationId ConversationId { get; private set; } = null!;

    /// <summary>Título descritivo da entrada de conhecimento sugerida.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Conteúdo completo da entrada — solução, insight ou padrão identificado.</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Fonte da sugestão (ex: "change-analysis", "error-diagnosis").</summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>Nível de relevância estimado pela IA, no intervalo [0, 1].</summary>
    public decimal Relevance { get; private set; }

    /// <summary>Estado atual de validação da entrada.</summary>
    public KnowledgeEntryStatus Status { get; private set; } = KnowledgeEntryStatus.Suggested;

    /// <summary>Identificador do responsável pela validação ou descarte. Null se pendente.</summary>
    public string? ValidatedBy { get; private set; }

    /// <summary>Data/hora UTC da validação ou descarte. Null se ainda não processada.</summary>
    public DateTimeOffset? ValidatedAt { get; private set; }

    /// <summary>Data/hora UTC em que a entrada foi sugerida pela IA.</summary>
    public DateTimeOffset SuggestedAt { get; private set; }

    /// <summary>
    /// Sugere uma nova entrada para a base de conhecimento com validações de invariantes.
    /// A entrada inicia em status Suggested, aguardando validação humana.
    /// </summary>
    public static Result<KnowledgeCaptureEntry> Suggest(
        AiConversationId conversationId,
        string title,
        string content,
        string source,
        decimal relevance,
        DateTimeOffset suggestedAt)
    {
        Guard.Against.Null(conversationId);
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(content);
        Guard.Against.NullOrWhiteSpace(source);

        if (relevance < 0m || relevance > 1m)
            return AiOrchestrationErrors.InvalidRelevance(relevance);

        return new KnowledgeCaptureEntry
        {
            Id = KnowledgeCaptureEntryId.New(),
            ConversationId = conversationId,
            Title = title,
            Content = content,
            Source = source,
            Relevance = relevance,
            SuggestedAt = suggestedAt,
            Status = KnowledgeEntryStatus.Suggested
        };
    }

    /// <summary>
    /// Valida a entrada, incorporando-a à base de conhecimento organizacional.
    /// Retorna erro se a entrada já foi processada.
    /// </summary>
    public Result<Unit> Validate(string validatedBy, DateTimeOffset validatedAt)
    {
        Guard.Against.NullOrWhiteSpace(validatedBy);

        if (Status != KnowledgeEntryStatus.Suggested)
            return AiOrchestrationErrors.EntryAlreadyProcessed(Id.Value.ToString());

        Status = KnowledgeEntryStatus.Validated;
        ValidatedBy = validatedBy;
        ValidatedAt = validatedAt;
        return Unit.Value;
    }

    /// <summary>
    /// Descarta a entrada por falta de relevância ou precisão.
    /// Retorna erro se a entrada já foi processada.
    /// </summary>
    public Result<Unit> Discard(string validatedBy, DateTimeOffset validatedAt)
    {
        Guard.Against.NullOrWhiteSpace(validatedBy);

        if (Status != KnowledgeEntryStatus.Suggested)
            return AiOrchestrationErrors.EntryAlreadyProcessed(Id.Value.ToString());

        Status = KnowledgeEntryStatus.Discarded;
        ValidatedBy = validatedBy;
        ValidatedAt = validatedAt;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de KnowledgeCaptureEntry.</summary>
public sealed record KnowledgeCaptureEntryId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static KnowledgeCaptureEntryId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static KnowledgeCaptureEntryId From(Guid id) => new(id);
}
