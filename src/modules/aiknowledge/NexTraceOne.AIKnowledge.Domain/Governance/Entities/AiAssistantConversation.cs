using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Representa uma conversa madura do assistente de IA governado do NexTraceOne.
/// Captura persona, escopo de contexto, modelo utilizado, tags e metadados
/// de sessão para uma experiência contextual, auditável e explicável.
///
/// Ciclo de vida: Active → (Archived).
///
/// Invariantes:
/// - Conversa inicia sempre em status Active com zero mensagens.
/// - Título é obrigatório e pode ser atualizado.
/// - Persona identifica o perfil funcional do utilizador na conversa.
/// - ContextScope define os domínios padrão de grounding.
/// </summary>
public sealed class AiAssistantConversation : AuditableEntity<AiAssistantConversationId>
{
    private AiAssistantConversation() { }

    /// <summary>Título descritivo da conversa.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Persona do utilizador nesta conversa.</summary>
    public string Persona { get; private set; } = string.Empty;

    /// <summary>Tipo de cliente que originou a conversa (Web, VsCode, Api, etc.).</summary>
    public AIClientType ClientType { get; private set; }

    /// <summary>Escopo de contexto padrão da conversa (ex: "services,contracts,incidents").</summary>
    public string DefaultContextScope { get; private set; } = string.Empty;

    /// <summary>Nome do último modelo utilizado na conversa.</summary>
    public string? LastModelUsed { get; private set; }

    /// <summary>Identificador do utilizador que criou a conversa.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>Número de mensagens na conversa.</summary>
    public int MessageCount { get; private set; }

    /// <summary>Tags separadas por vírgula para classificação (ex: "troubleshooting,payment").</summary>
    public string Tags { get; private set; } = string.Empty;

    /// <summary>Indica se a conversa está ativa.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Data/hora UTC da última mensagem.</summary>
    public DateTimeOffset? LastMessageAt { get; private set; }

    /// <summary>Identificador opcional do serviço associado à conversa.</summary>
    public Guid? ServiceId { get; private set; }

    /// <summary>Identificador opcional do contrato associado à conversa.</summary>
    public Guid? ContractId { get; private set; }

    /// <summary>Identificador opcional do incidente associado à conversa.</summary>
    public Guid? IncidentId { get; private set; }

    /// <summary>Identificador opcional da alteração associada à conversa.</summary>
    public Guid? ChangeId { get; private set; }

    /// <summary>Identificador opcional da equipa associada à conversa.</summary>
    public Guid? TeamId { get; private set; }

    /// <summary>
    /// Inicia uma nova conversa do assistente de IA com metadados completos de sessão.
    /// </summary>
    public static AiAssistantConversation Start(
        string title,
        string persona,
        AIClientType clientType,
        string defaultContextScope,
        string createdBy,
        Guid? serviceId = null,
        Guid? contractId = null,
        Guid? incidentId = null,
        Guid? teamId = null,
        Guid? changeId = null)
    {
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(persona);
        Guard.Against.NullOrWhiteSpace(createdBy);

        return new AiAssistantConversation
        {
            Id = AiAssistantConversationId.New(),
            Title = title,
            Persona = persona,
            ClientType = clientType,
            DefaultContextScope = defaultContextScope ?? string.Empty,
            CreatedBy = createdBy,
            MessageCount = 0,
            Tags = string.Empty,
            IsActive = true,
            ServiceId = serviceId,
            ContractId = contractId,
            IncidentId = incidentId,
            ChangeId = changeId,
            TeamId = teamId
        };
    }

    /// <summary>
    /// Regista uma nova mensagem na conversa, incrementando o contador e atualizando timestamps.
    /// </summary>
    public Result<Unit> RecordMessage(string? modelUsed, DateTimeOffset messageAt)
    {
        if (!IsActive)
            return AiGovernanceErrors.ConversationNotActive(Id.Value.ToString());

        MessageCount++;
        LastMessageAt = messageAt;
        if (!string.IsNullOrWhiteSpace(modelUsed))
            LastModelUsed = modelUsed;

        return Unit.Value;
    }

    /// <summary>
    /// Atualiza o título e tags da conversa.
    /// </summary>
    public Result<Unit> UpdateMetadata(string title, string? tags)
    {
        Guard.Against.NullOrWhiteSpace(title);

        Title = title;
        Tags = tags ?? string.Empty;
        return Unit.Value;
    }

    /// <summary>
    /// Arquiva a conversa, tornando-a inativa.
    /// Operação idempotente.
    /// </summary>
    public Result<Unit> Archive()
    {
        IsActive = false;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de AiAssistantConversation.</summary>
public sealed record AiAssistantConversationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AiAssistantConversationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AiAssistantConversationId From(Guid id) => new(id);
}
