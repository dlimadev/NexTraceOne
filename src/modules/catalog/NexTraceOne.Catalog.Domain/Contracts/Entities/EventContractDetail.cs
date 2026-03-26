using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade específica para metadados de contratos Event/AsyncAPI publicados.
/// Captura informações estruturais extraídas da spec AsyncAPI que não existem em contratos REST:
/// channels (topics), operações (publish/subscribe), servidores/brokers, content type e mensagens.
/// Vinculada a uma ContractVersion com Protocol = AsyncApi.
/// </summary>
public sealed class EventContractDetail : AuditableEntity<EventContractDetailId>
{
    private EventContractDetail() { }

    /// <summary>Identificador da versão de contrato AsyncAPI à qual este detalhe pertence.</summary>
    public ContractVersionId ContractVersionId { get; private set; } = null!;

    /// <summary>Título do serviço event-driven (info.title da spec AsyncAPI).</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Versão do protocolo AsyncAPI usado na spec (ex: "2.6.0" ou "3.0.0").</summary>
    public string AsyncApiVersion { get; private set; } = "2.6.0";

    /// <summary>Tipo de conteúdo padrão das mensagens (defaultContentType, ex: "application/json").</summary>
    public string DefaultContentType { get; private set; } = "application/json";

    /// <summary>
    /// JSON serializado dos canais/tópicos extraídos da spec AsyncAPI.
    /// Formato: { "channelName": ["publish", "subscribe"], ... }
    /// </summary>
    public string ChannelsJson { get; private set; } = "{}";

    /// <summary>
    /// JSON serializado das mensagens/schemas extraídos da spec AsyncAPI.
    /// Formato: { "messageName": ["field1", "field2"], ... }
    /// </summary>
    public string MessagesJson { get; private set; } = "{}";

    /// <summary>
    /// JSON serializado dos servidores/brokers definidos na spec AsyncAPI.
    /// Formato: { "serverName": "url", ... }
    /// </summary>
    public string ServersJson { get; private set; } = "{}";

    /// <summary>
    /// Cria um novo EventContractDetail a partir de dados extraídos da spec AsyncAPI.
    /// </summary>
    public static Result<EventContractDetail> Create(
        ContractVersionId contractVersionId,
        string title,
        string asyncApiVersion,
        string channelsJson,
        string messagesJson,
        string serversJson,
        string defaultContentType = "application/json")
    {
        Guard.Against.Null(contractVersionId);
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(asyncApiVersion);

        return new EventContractDetail
        {
            Id = EventContractDetailId.New(),
            ContractVersionId = contractVersionId,
            Title = title,
            AsyncApiVersion = asyncApiVersion,
            ChannelsJson = channelsJson,
            MessagesJson = messagesJson,
            ServersJson = serversJson,
            DefaultContentType = defaultContentType
        };
    }

    /// <summary>Atualiza os metadados AsyncAPI extraídos após re-parsing da spec.</summary>
    public void UpdateFromParsing(
        string title,
        string asyncApiVersion,
        string channelsJson,
        string messagesJson,
        string serversJson,
        string defaultContentType)
    {
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(asyncApiVersion);

        Title = title;
        AsyncApiVersion = asyncApiVersion;
        ChannelsJson = channelsJson;
        MessagesJson = messagesJson;
        ServersJson = serversJson;
        DefaultContentType = defaultContentType;
    }
}

/// <summary>Identificador fortemente tipado de EventContractDetail.</summary>
public sealed record EventContractDetailId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static EventContractDetailId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static EventContractDetailId From(Guid id) => new(id);
}
