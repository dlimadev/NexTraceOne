using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para IngestionSource.
/// </summary>
public sealed record IngestionSourceId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Representa uma fonte de ingestão de dados associada a um conector.
/// Cada conector pode ter múltiplas fontes (ex: GitHub pode ter webhooks + polling).
/// </summary>
public sealed class IngestionSource : Entity<IngestionSourceId>
{
    /// <summary>Conector ao qual esta fonte pertence.</summary>
    public IntegrationConnectorId ConnectorId { get; private init; } = null!;

    /// <summary>Nome da fonte (ex: "Webhook", "API Polling").</summary>
    public string Name { get; private init; } = string.Empty;

    /// <summary>Tipo de fonte (ex: "Webhook", "API Polling", "Stream").</summary>
    public string SourceType { get; private set; } = string.Empty;

    /// <summary>Data domain this source provides data for (ex: "Changes", "Incidents", "Runtime").</summary>
    public string DataDomain { get; private set; } = string.Empty;

    /// <summary>Descrição da fonte e seu propósito.</summary>
    public string? Description { get; private set; }

    /// <summary>Endpoint de conexão.</summary>
    public string? Endpoint { get; private set; }

    /// <summary>Nível de confiança da fonte.</summary>
    public SourceTrustLevel TrustLevel { get; private set; }

    /// <summary>Estado de frescura da fonte.</summary>
    public FreshnessStatus FreshnessStatus { get; private set; }

    /// <summary>Estado operacional da fonte.</summary>
    public SourceStatus Status { get; private set; }

    /// <summary>Data/hora UTC do último dado recebido.</summary>
    public DateTimeOffset? LastDataReceivedAt { get; private set; }

    /// <summary>Data/hora UTC da última conclusão de processamento.</summary>
    public DateTimeOffset? LastProcessedAt { get; private set; }

    /// <summary>Contagem de itens processados.</summary>
    public long DataItemsProcessed { get; private set; }

    /// <summary>Intervalo esperado em minutos entre dados.</summary>
    public int? ExpectedIntervalMinutes { get; private set; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última atualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    private IngestionSource() { }

    /// <summary>
    /// Cria uma nova fonte de ingestão.
    /// </summary>
    public static IngestionSource Create(
        IntegrationConnectorId connectorId,
        string name,
        string sourceType,
        string? dataDomain,
        string? description,
        string? endpoint,
        int? expectedIntervalMinutes,
        DateTimeOffset utcNow)
    {
        Guard.Against.Null(connectorId, nameof(connectorId));
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 200, nameof(name));
        Guard.Against.NullOrWhiteSpace(sourceType, nameof(sourceType));
        Guard.Against.StringTooLong(sourceType, 100, nameof(sourceType));

        return new IngestionSource
        {
            Id = new IngestionSourceId(Guid.NewGuid()),
            ConnectorId = connectorId,
            Name = name.Trim(),
            SourceType = sourceType.Trim(),
            DataDomain = dataDomain?.Trim() ?? sourceType.Trim(),
            Description = description?.Trim(),
            Endpoint = endpoint?.Trim(),
            TrustLevel = SourceTrustLevel.Unverified,
            FreshnessStatus = FreshnessStatus.Unknown,
            Status = SourceStatus.Active,
            DataItemsProcessed = 0,
            ExpectedIntervalMinutes = expectedIntervalMinutes,
            CreatedAt = utcNow
        };
    }

    /// <summary>Regista receção de dados.</summary>
    public void RecordDataReceived(int itemCount, DateTimeOffset utcNow)
    {
        LastDataReceivedAt = utcNow;
        LastProcessedAt = utcNow;
        DataItemsProcessed += itemCount;
        UpdateFreshnessStatus(utcNow);
        UpdatedAt = utcNow;
    }

    /// <summary>Regista conclusão de processamento sem nova receção de dados.</summary>
    public void RecordProcessingCompleted(DateTimeOffset utcNow)
    {
        LastProcessedAt = utcNow;
        UpdatedAt = utcNow;
    }

    /// <summary>Promove o nível de confiança.</summary>
    public void PromoteTrustLevel(SourceTrustLevel newLevel, DateTimeOffset utcNow)
    {
        TrustLevel = newLevel;
        UpdatedAt = utcNow;
    }

    /// <summary>Marca como erro.</summary>
    public void MarkError(DateTimeOffset utcNow)
    {
        Status = SourceStatus.Error;
        FreshnessStatus = FreshnessStatus.Stale;
        UpdatedAt = utcNow;
    }

    /// <summary>Desativa a fonte.</summary>
    public void Disable(DateTimeOffset utcNow)
    {
        Status = SourceStatus.Disabled;
        UpdatedAt = utcNow;
    }

    /// <summary>Ativa a fonte.</summary>
    public void Activate(DateTimeOffset utcNow)
    {
        Status = SourceStatus.Active;
        UpdatedAt = utcNow;
    }

    /// <summary>Atualiza o domínio de dados da fonte.</summary>
    public void UpdateDataDomain(string dataDomain, DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(dataDomain, nameof(dataDomain));
        DataDomain = dataDomain.Trim();
        UpdatedAt = utcNow;
    }

    private void UpdateFreshnessStatus(DateTimeOffset utcNow)
    {
        if (LastDataReceivedAt is null)
        {
            FreshnessStatus = FreshnessStatus.Unknown;
            return;
        }

        var lag = utcNow - LastDataReceivedAt.Value;
        var expectedMinutes = ExpectedIntervalMinutes ?? 30;

        FreshnessStatus = lag.TotalMinutes switch
        {
            var m when m < expectedMinutes => FreshnessStatus.Fresh,
            var m when m < expectedMinutes * 4 => FreshnessStatus.Stale,
            var m when m < expectedMinutes * 12 => FreshnessStatus.Outdated,
            _ => FreshnessStatus.Expired
        };
    }
}
