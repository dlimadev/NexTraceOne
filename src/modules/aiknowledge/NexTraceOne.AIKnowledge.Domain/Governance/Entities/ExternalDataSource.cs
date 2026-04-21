using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Representa uma fonte de dados externa configurada para enriquecer o pipeline RAG da IA.
/// Cada fonte define um conector (GitHub, Brave Search, GitLab, directório, etc.)
/// com a respectiva configuração JSON e política de sincronização.
///
/// Invariantes:
/// - Nome e tipo de conector são obrigatórios.
/// - ConnectorConfigJson deve ser JSON válido não vazio.
/// - Prioridade deve ser não-negativa (menor = maior prioridade).
/// - SyncIntervalMinutes = 0 significa sincronização apenas manual.
/// </summary>
public sealed class ExternalDataSource : AuditableEntity<ExternalDataSourceId>
{
    private ExternalDataSource() { }

    /// <summary>Nome identificador da fonte de dados externa.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição do conteúdo e propósito da fonte.</summary>
    public string? Description { get; private set; }

    /// <summary>Tipo de conector que determina como o conteúdo é obtido.</summary>
    public ExternalDataSourceConnectorType ConnectorType { get; private set; }

    /// <summary>
    /// Configuração JSON específica do conector (credenciais, URLs, filtros).
    /// Estrutura varia por ConnectorType — chaves sensíveis devem ser encriptadas na camada de infra.
    /// </summary>
    public string ConnectorConfigJson { get; private set; } = string.Empty;

    /// <summary>Indica se a fonte está activa e incluída no pipeline RAG.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Prioridade — valor menor indica maior preferência de uso. Mínimo 0.</summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Intervalo de sincronização automática em minutos.
    /// Zero indica sincronização apenas manual via API.
    /// </summary>
    public int SyncIntervalMinutes { get; private set; }

    /// <summary>Data/hora UTC da última sincronização bem-sucedida.</summary>
    public DateTimeOffset? LastSyncedAt { get; private set; }

    /// <summary>Estado da última sincronização: Pending, Success ou Error.</summary>
    public string? LastSyncStatus { get; private set; }

    /// <summary>Mensagem de erro da última sincronização falhada.</summary>
    public string? LastSyncError { get; private set; }

    /// <summary>Total de documentos indexados na última sincronização.</summary>
    public int LastSyncDocumentCount { get; private set; }

    /// <summary>Data/hora UTC em que a fonte foi registada.</summary>
    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>
    /// Regista uma nova fonte de dados externa com validações de invariantes.
    /// A fonte inicia activa com status de sincronização Pending.
    /// </summary>
    public static ExternalDataSource Register(
        string name,
        string? description,
        ExternalDataSourceConnectorType connectorType,
        string connectorConfigJson,
        int priority,
        int syncIntervalMinutes,
        DateTimeOffset registeredAt)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(connectorConfigJson);
        Guard.Against.Negative(priority);
        Guard.Against.Negative(syncIntervalMinutes);

        return new ExternalDataSource
        {
            Id = ExternalDataSourceId.New(),
            Name = name,
            Description = description,
            ConnectorType = connectorType,
            ConnectorConfigJson = connectorConfigJson,
            IsActive = true,
            Priority = priority,
            SyncIntervalMinutes = syncIntervalMinutes,
            LastSyncStatus = "Pending",
            RegisteredAt = registeredAt
        };
    }

    /// <summary>
    /// Actualiza a configuração da fonte de dados.
    /// </summary>
    public Result<Unit> Update(
        string? description,
        string connectorConfigJson,
        int priority,
        int syncIntervalMinutes)
    {
        Guard.Against.NullOrWhiteSpace(connectorConfigJson);
        Guard.Against.Negative(priority);
        Guard.Against.Negative(syncIntervalMinutes);

        Description = description;
        ConnectorConfigJson = connectorConfigJson;
        Priority = priority;
        SyncIntervalMinutes = syncIntervalMinutes;
        return Unit.Value;
    }

    /// <summary>Activa a fonte, tornando-a disponível no pipeline RAG.</summary>
    public Result<Unit> Activate()
    {
        IsActive = true;
        return Unit.Value;
    }

    /// <summary>Desactiva a fonte, removendo-a do pipeline RAG.</summary>
    public Result<Unit> Deactivate()
    {
        IsActive = false;
        return Unit.Value;
    }

    /// <summary>Regista o resultado bem-sucedido de uma sincronização.</summary>
    public void RecordSyncSuccess(int documentCount, DateTimeOffset syncedAt)
    {
        LastSyncedAt = syncedAt;
        LastSyncStatus = "Success";
        LastSyncError = null;
        LastSyncDocumentCount = documentCount;
    }

    /// <summary>Regista o resultado falhado de uma sincronização.</summary>
    public void RecordSyncError(string error, DateTimeOffset syncedAt)
    {
        Guard.Against.NullOrWhiteSpace(error);
        LastSyncedAt = syncedAt;
        LastSyncStatus = "Error";
        LastSyncError = error;
    }
}

/// <summary>Identificador fortemente tipado de ExternalDataSource.</summary>
public sealed record ExternalDataSourceId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ExternalDataSourceId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ExternalDataSourceId From(Guid id) => new(id);
}
