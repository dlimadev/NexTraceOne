using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ExternalAi.Domain.Enums;
using NexTraceOne.ExternalAi.Domain.Errors;

namespace NexTraceOne.ExternalAi.Domain.Entities;

/// <summary>
/// Representa conhecimento organizacional capturado a partir de interações com provedores
/// externos de IA. Permite reutilização de soluções e insights gerados por IA para
/// problemas recorrentes como resolução de erros, classificação de mudanças e padrões.
///
/// Ciclo de vida: Pending → (Approved | Rejected).
///
/// Invariantes:
/// - Cada captura está vinculada a uma consulta de IA específica.
/// - Revisão é obrigatória — Approve e Reject requerem identificação do revisor.
/// - Uma vez revisada, não pode ser revisada novamente.
/// - Contagem de reutilização só incrementa para capturas aprovadas.
/// </summary>
public sealed class KnowledgeCapture : AuditableEntity<KnowledgeCaptureId>
{
    private KnowledgeCapture() { }

    /// <summary>Identificador da consulta de IA que originou este conhecimento.</summary>
    public ExternalAiConsultationId ConsultationId { get; private set; } = null!;

    /// <summary>Título descritivo do conhecimento capturado.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Conteúdo completo do conhecimento — solução, insight ou padrão identificado.</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Categoria do conhecimento (ex: "error-resolution", "change-classification").</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>Tags de classificação separadas por vírgula para busca e filtragem.</summary>
    public string Tags { get; private set; } = string.Empty;

    /// <summary>Estado atual de revisão do conhecimento capturado.</summary>
    public KnowledgeStatus Status { get; private set; } = KnowledgeStatus.Pending;

    /// <summary>Identificador do revisor que aprovou ou rejeitou o conhecimento. Null se pendente.</summary>
    public string? ReviewedBy { get; private set; }

    /// <summary>Data/hora UTC da revisão. Null se ainda não revisado.</summary>
    public DateTimeOffset? ReviewedAt { get; private set; }

    /// <summary>Motivo da rejeição, preenchido quando o conhecimento é rejeitado. Null se pendente ou aprovado.</summary>
    public string? RejectionReason { get; private set; }

    /// <summary>Número de vezes que este conhecimento foi reutilizado em outras interações.</summary>
    public int ReuseCount { get; private set; }

    /// <summary>Data/hora UTC em que o conhecimento foi capturado.</summary>
    public DateTimeOffset CapturedAt { get; private set; }

    /// <summary>
    /// Captura conhecimento organizacional a partir de uma consulta de IA.
    /// Inicia em status Pending com contagem de reutilização zerada.
    /// </summary>
    public static KnowledgeCapture Capture(
        ExternalAiConsultationId consultationId,
        string title,
        string content,
        string category,
        string tags,
        DateTimeOffset capturedAt)
    {
        Guard.Against.Null(consultationId);
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(content);
        Guard.Against.NullOrWhiteSpace(category);
        Guard.Against.NullOrWhiteSpace(tags);

        return new KnowledgeCapture
        {
            Id = KnowledgeCaptureId.New(),
            ConsultationId = consultationId,
            Title = title,
            Content = content,
            Category = category,
            Tags = tags,
            CapturedAt = capturedAt,
            Status = KnowledgeStatus.Pending,
            ReuseCount = 0
        };
    }

    /// <summary>
    /// Aprova o conhecimento capturado, tornando-o disponível para reutilização.
    /// Retorna erro se já foi revisado anteriormente.
    /// </summary>
    public Result<Unit> Approve(string reviewedBy, DateTimeOffset reviewedAt)
    {
        Guard.Against.NullOrWhiteSpace(reviewedBy);

        if (Status != KnowledgeStatus.Pending)
            return ExternalAiErrors.KnowledgeAlreadyReviewed(Id.Value.ToString());

        Status = KnowledgeStatus.Approved;
        ReviewedBy = reviewedBy;
        ReviewedAt = reviewedAt;
        return Unit.Value;
    }

    /// <summary>
    /// Rejeita o conhecimento capturado com motivo obrigatório.
    /// Retorna erro se já foi revisado anteriormente.
    /// </summary>
    public Result<Unit> Reject(string reviewedBy, string reason, DateTimeOffset reviewedAt)
    {
        Guard.Against.NullOrWhiteSpace(reviewedBy);
        Guard.Against.NullOrWhiteSpace(reason);

        if (Status != KnowledgeStatus.Pending)
            return ExternalAiErrors.KnowledgeAlreadyReviewed(Id.Value.ToString());

        Status = KnowledgeStatus.Rejected;
        ReviewedBy = reviewedBy;
        ReviewedAt = reviewedAt;
        RejectionReason = reason;
        return Unit.Value;
    }

    /// <summary>
    /// Incrementa o contador de reutilização deste conhecimento.
    /// Apenas conhecimentos aprovados podem ter a reutilização contabilizada.
    /// </summary>
    public Result<Unit> IncrementReuse()
    {
        if (Status != KnowledgeStatus.Approved)
            return ExternalAiErrors.KnowledgeNotApproved(Id.Value.ToString());

        ReuseCount++;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de KnowledgeCapture.</summary>
public sealed record KnowledgeCaptureId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static KnowledgeCaptureId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static KnowledgeCaptureId From(Guid id) => new(id);
}
