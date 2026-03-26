using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que armazena metadados AsyncAPI específicos para um ContractDraft em edição.
/// Permite que o Contract Studio mantenha informações do evento (título, versão AsyncAPI,
/// channels, mensagens, content type) desacopladas do SpecContent genérico do draft.
/// Vinculada a um ContractDraft com Protocol = AsyncApi e ContractType = Event.
/// </summary>
public sealed class EventDraftMetadata : Entity<EventDraftMetadataId>
{
    private EventDraftMetadata() { }

    /// <summary>Identificador do draft de contrato de evento ao qual este metadado pertence.</summary>
    public ContractDraftId ContractDraftId { get; private set; } = null!;

    /// <summary>Título do serviço event-driven definido no draft.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Versão do protocolo AsyncAPI usado no draft.</summary>
    public string AsyncApiVersion { get; private set; } = "2.6.0";

    /// <summary>Tipo de conteúdo padrão das mensagens.</summary>
    public string DefaultContentType { get; private set; } = "application/json";

    /// <summary>
    /// JSON serializado dos channels/topics definidos no draft pelo editor visual.
    /// Formato: { "channelName": ["publish", "subscribe"], ... }
    /// </summary>
    public string ChannelsJson { get; private set; } = "{}";

    /// <summary>
    /// JSON serializado das mensagens/schemas definidos no draft.
    /// Formato: { "messageName": ["field1", "field2"], ... }
    /// </summary>
    public string MessagesJson { get; private set; } = "{}";

    /// <summary>
    /// Cria novos metadados de evento para um draft de contrato.
    /// </summary>
    public static EventDraftMetadata Create(
        ContractDraftId contractDraftId,
        string title,
        string asyncApiVersion = "2.6.0",
        string defaultContentType = "application/json",
        string channelsJson = "{}",
        string messagesJson = "{}")
    {
        Guard.Against.Null(contractDraftId);
        Guard.Against.NullOrWhiteSpace(title);

        return new EventDraftMetadata
        {
            Id = EventDraftMetadataId.New(),
            ContractDraftId = contractDraftId,
            Title = title,
            AsyncApiVersion = asyncApiVersion,
            DefaultContentType = defaultContentType,
            ChannelsJson = channelsJson,
            MessagesJson = messagesJson
        };
    }

    /// <summary>Atualiza os metadados AsyncAPI do draft quando o utilizador edita no Studio visual.</summary>
    public void Update(
        string title,
        string asyncApiVersion,
        string defaultContentType,
        string channelsJson,
        string messagesJson)
    {
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(asyncApiVersion);

        Title = title;
        AsyncApiVersion = asyncApiVersion;
        DefaultContentType = defaultContentType;
        ChannelsJson = channelsJson;
        MessagesJson = messagesJson;
    }
}

/// <summary>Identificador fortemente tipado de EventDraftMetadata.</summary>
public sealed record EventDraftMetadataId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static EventDraftMetadataId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static EventDraftMetadataId From(Guid id) => new(id);
}
