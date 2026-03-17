using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.AIKnowledge.Domain.Orchestration.Enums;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Errors;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;

/// <summary>
/// Representa uma conversa multi-turno com IA sobre uma mudança, release ou erro específico.
/// Cada conversa acumula turnos de interação e pode gerar um resumo ao ser concluída,
/// capturando o conhecimento obtido durante a investigação assistida por IA.
///
/// Ciclo de vida: Active → (Completed | Expired).
///
/// Invariantes:
/// - Conversa inicia sempre em status Active com zero turnos.
/// - Novos turnos só podem ser adicionados a conversas ativas.
/// - Conclusão requer resumo obrigatório para captura de conhecimento.
/// - Expiração ocorre por inatividade — encerra a conversa sem resumo.
/// </summary>
public sealed class AiConversation : AuditableEntity<AiConversationId>
{
    private AiConversation() { }

    /// <summary>Identificador opcional da release associada a esta conversa.</summary>
    public Guid? ReleaseId { get; private set; }

    /// <summary>Nome do serviço que é tema da conversa.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Tópico principal da conversa (ex: "análise de impacto", "diagnóstico de erro").</summary>
    public string Topic { get; private set; } = string.Empty;

    /// <summary>Número de turnos de interação realizados na conversa.</summary>
    public int TurnCount { get; private set; }

    /// <summary>Estado atual do ciclo de vida da conversa.</summary>
    public ConversationStatus Status { get; private set; } = ConversationStatus.Active;

    /// <summary>Identificador do usuário que iniciou a conversa.</summary>
    public string StartedBy { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC em que a conversa foi iniciada.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>Data/hora UTC do último turno de interação. Null se nenhum turno foi adicionado.</summary>
    public DateTimeOffset? LastTurnAt { get; private set; }

    /// <summary>Resumo gerado ao concluir a conversa. Null se ainda ativa ou expirada.</summary>
    public string? Summary { get; private set; }

    /// <summary>
    /// Inicia uma nova conversa multi-turno com IA sobre um serviço e tópico específicos.
    /// A conversa inicia em status Active com zero turnos.
    /// </summary>
    public static AiConversation Start(
        string serviceName,
        string topic,
        string startedBy,
        DateTimeOffset startedAt,
        Guid? releaseId = null)
    {
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(topic);
        Guard.Against.NullOrWhiteSpace(startedBy);

        return new AiConversation
        {
            Id = AiConversationId.New(),
            ServiceName = serviceName,
            Topic = topic,
            StartedBy = startedBy,
            StartedAt = startedAt,
            ReleaseId = releaseId,
            TurnCount = 0,
            Status = ConversationStatus.Active
        };
    }

    /// <summary>
    /// Registra um novo turno de interação na conversa.
    /// Incrementa o contador de turnos e atualiza a data do último turno.
    /// Retorna erro se a conversa não estiver ativa.
    /// </summary>
    public Result<Unit> AddTurn(DateTimeOffset turnAt)
    {
        if (Status != ConversationStatus.Active)
            return AiOrchestrationErrors.ConversationNotActive(Id.Value.ToString());

        TurnCount++;
        LastTurnAt = turnAt;
        return Unit.Value;
    }

    /// <summary>
    /// Conclui a conversa com um resumo obrigatório para captura de conhecimento.
    /// Transiciona o status para Completed.
    /// Retorna erro se a conversa já foi concluída ou expirou.
    /// </summary>
    public Result<Unit> Complete(string summary, DateTimeOffset completedAt)
    {
        Guard.Against.NullOrWhiteSpace(summary);

        if (Status != ConversationStatus.Active)
            return AiOrchestrationErrors.ConversationAlreadyCompleted(Id.Value.ToString());

        Summary = summary;
        LastTurnAt = completedAt;
        Status = ConversationStatus.Completed;
        return Unit.Value;
    }

    /// <summary>
    /// Marca a conversa como expirada por inatividade ou timeout.
    /// Retorna erro se a conversa já foi concluída ou expirou.
    /// </summary>
    public Result<Unit> Expire(DateTimeOffset expiredAt)
    {
        if (Status != ConversationStatus.Active)
            return AiOrchestrationErrors.ConversationAlreadyCompleted(Id.Value.ToString());

        LastTurnAt = expiredAt;
        Status = ConversationStatus.Expired;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de AiConversation.</summary>
public sealed record AiConversationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AiConversationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AiConversationId From(Guid id) => new(id);
}
