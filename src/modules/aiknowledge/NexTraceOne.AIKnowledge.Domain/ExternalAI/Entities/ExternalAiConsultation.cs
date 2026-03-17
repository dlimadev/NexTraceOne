using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.AIKnowledge.Domain.ExternalAI.Enums;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Errors;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;

/// <summary>
/// Representa uma consulta enviada a um provedor externo de IA para análise de mudanças,
/// diagnóstico de erros ou geração de cenários de teste. Cada consulta é associada a um
/// provedor, contém o contexto da pergunta e rastreia tokens consumidos e confiança.
///
/// Ciclo de vida: Pending → InProgress → (Completed | Failed).
///
/// Invariantes:
/// - Consulta inicia sempre em status Pending com resposta nula.
/// - Confiança deve estar no intervalo [0, 1] ao registrar resposta.
/// - Transições de estado são unidirecionais — não permite reverter para Pending.
/// - Resposta só pode ser registrada uma vez (idempotent-safe).
/// </summary>
public sealed class ExternalAiConsultation : AuditableEntity<ExternalAiConsultationId>
{
    private ExternalAiConsultation() { }

    /// <summary>Identificador do provedor de IA utilizado para esta consulta.</summary>
    public ExternalAiProviderId ProviderId { get; private set; } = null!;

    /// <summary>Contexto da consulta — descreve a mudança, release ou erro sendo analisado.</summary>
    public string Context { get; private set; } = string.Empty;

    /// <summary>Pergunta ou instrução enviada ao provedor de IA.</summary>
    public string Query { get; private set; } = string.Empty;

    /// <summary>Resposta recebida do provedor. Null enquanto não completada.</summary>
    public string? Response { get; private set; }

    /// <summary>Quantidade de tokens consumidos na interação com o provedor.</summary>
    public int TokensUsed { get; private set; }

    /// <summary>Estado atual do ciclo de vida da consulta.</summary>
    public ConsultationStatus Status { get; private set; } = ConsultationStatus.Pending;

    /// <summary>Identificador do usuário que solicitou a consulta.</summary>
    public string RequestedBy { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC em que a consulta foi solicitada.</summary>
    public DateTimeOffset RequestedAt { get; private set; }

    /// <summary>Data/hora UTC em que a consulta foi completada ou falhou. Null se pendente.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Nível de confiança da resposta da IA, no intervalo [0, 1]. Zero se pendente.</summary>
    public decimal Confidence { get; private set; }

    /// <summary>
    /// Cria uma nova consulta a provedor externo de IA com validações de invariantes.
    /// A consulta inicia em status Pending, sem resposta e com confiança zero.
    /// </summary>
    public static ExternalAiConsultation Create(
        ExternalAiProviderId providerId,
        string context,
        string query,
        string requestedBy,
        DateTimeOffset requestedAt)
    {
        Guard.Against.Null(providerId);
        Guard.Against.NullOrWhiteSpace(context);
        Guard.Against.NullOrWhiteSpace(query);
        Guard.Against.NullOrWhiteSpace(requestedBy);

        return new ExternalAiConsultation
        {
            Id = ExternalAiConsultationId.New(),
            ProviderId = providerId,
            Context = context,
            Query = query,
            RequestedBy = requestedBy,
            RequestedAt = requestedAt,
            Status = ConsultationStatus.Pending,
            TokensUsed = 0,
            Confidence = 0m
        };
    }

    /// <summary>
    /// Registra a resposta recebida do provedor de IA com tokens consumidos e confiança.
    /// Transiciona o status para Completed e registra a data de conclusão.
    /// Retorna erro se a consulta já foi completada ou falhou.
    /// </summary>
    public Result<Unit> RecordResponse(string response, int tokensUsed, decimal confidence, DateTimeOffset completedAt)
    {
        Guard.Against.NullOrWhiteSpace(response);
        Guard.Against.Negative(tokensUsed);

        if (confidence < 0m || confidence > 1m)
            return ExternalAiErrors.InvalidConfidence(confidence);

        if (Status == ConsultationStatus.Completed)
            return ExternalAiErrors.ConsultationAlreadyCompleted(Id.Value.ToString());

        if (Status == ConsultationStatus.Failed)
            return ExternalAiErrors.ConsultationAlreadyFailed(Id.Value.ToString());

        Response = response;
        TokensUsed = tokensUsed;
        Confidence = confidence;
        CompletedAt = completedAt;
        Status = ConsultationStatus.Completed;
        return Unit.Value;
    }

    /// <summary>
    /// Marca a consulta como falha com registro do motivo e data.
    /// Retorna erro se a consulta já foi completada ou já havia falhado.
    /// </summary>
    public Result<Unit> MarkFailed(string reason, DateTimeOffset failedAt)
    {
        Guard.Against.NullOrWhiteSpace(reason);

        if (Status == ConsultationStatus.Completed)
            return ExternalAiErrors.ConsultationAlreadyCompleted(Id.Value.ToString());

        if (Status == ConsultationStatus.Failed)
            return ExternalAiErrors.ConsultationAlreadyFailed(Id.Value.ToString());

        Response = reason;
        CompletedAt = failedAt;
        Status = ConsultationStatus.Failed;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de ExternalAiConsultation.</summary>
public sealed record ExternalAiConsultationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ExternalAiConsultationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ExternalAiConsultationId From(Guid id) => new(id);
}
