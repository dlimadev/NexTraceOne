using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

/// <summary>
/// Entidade que representa o resultado persistido de uma correlação dinâmica entre
/// um incidente operacional e uma mudança (release) identificada pelo motor de correlação.
/// Gerada pelo handler CorrelateIncidentWithChanges e consultada por GetCorrelatedChanges.
/// </summary>
public sealed class IncidentChangeCorrelation : AuditableEntity<IncidentChangeCorrelationId>
{
    private IncidentChangeCorrelation() { }

    /// <summary>Identificador do incidente correlacionado.</summary>
    public Guid IncidentId { get; private set; }

    /// <summary>Identificador da mudança (release) correlacionada.</summary>
    public Guid ChangeId { get; private set; }

    /// <summary>Identificador do serviço que originou a correspondência.</summary>
    public Guid ServiceId { get; private set; }

    /// <summary>Nível de confiança calculado pelo motor de correlação.</summary>
    public CorrelationConfidenceLevel ConfidenceLevel { get; private set; }

    /// <summary>Tipo de correspondência que determinou a correlação.</summary>
    public CorrelationMatchType MatchType { get; private set; }

    /// <summary>Janela temporal (em horas antes do incidente) usada na correlação.</summary>
    public int TimeWindowHours { get; private set; }

    /// <summary>Data/hora UTC em que a correlação foi computada e persistida.</summary>
    public DateTimeOffset CorrelatedAt { get; private set; }

    /// <summary>Identificador do tenant ao qual a correlação pertence.</summary>
    public Guid? TenantId { get; private set; }

    // ── Metadados da mudança (denormalizados para consulta eficiente) ──────

    /// <summary>Nome do serviço da mudança correlacionada.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Descrição ou sumário da mudança correlacionada.</summary>
    public string ChangeDescription { get; private set; } = string.Empty;

    /// <summary>Ambiente da mudança correlacionada.</summary>
    public string ChangeEnvironment { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC em que a mudança ocorreu.</summary>
    public DateTimeOffset ChangeOccurredAt { get; private set; }

    // ── Legacy asset metadata (para correlações com ativos mainframe/legacy) ──

    /// <summary>Tipo do ativo legacy correlacionado (e.g. BatchJob, MqQueue).</summary>
    public string? LegacyAssetType { get; private set; }

    /// <summary>Nome do ativo legacy correlacionado.</summary>
    public string? LegacyAssetName { get; private set; }

    /// <summary>Identificador do ativo legacy no catálogo, quando disponível.</summary>
    public Guid? LegacyAssetId { get; private set; }

    /// <summary>
    /// Define informações do ativo legacy correlacionado.
    /// </summary>
    public void SetLegacyAsset(string assetType, string assetName, Guid? assetId)
    {
        LegacyAssetType = assetType;
        LegacyAssetName = assetName;
        LegacyAssetId = assetId;
    }

    /// <summary>
    /// Cria uma nova correlação entre incidente e mudança.
    /// </summary>
    public static IncidentChangeCorrelation Create(
        Guid incidentId,
        Guid changeId,
        Guid serviceId,
        CorrelationConfidenceLevel confidenceLevel,
        CorrelationMatchType matchType,
        int timeWindowHours,
        DateTimeOffset correlatedAt,
        Guid? tenantId,
        string serviceName,
        string changeDescription,
        string changeEnvironment,
        DateTimeOffset changeOccurredAt)
    {
        Guard.Against.Default(incidentId);
        Guard.Against.Default(changeId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NegativeOrZero(timeWindowHours);

        return new IncidentChangeCorrelation
        {
            Id = IncidentChangeCorrelationId.New(),
            IncidentId = incidentId,
            ChangeId = changeId,
            ServiceId = serviceId,
            ConfidenceLevel = confidenceLevel,
            MatchType = matchType,
            TimeWindowHours = timeWindowHours,
            CorrelatedAt = correlatedAt,
            TenantId = tenantId,
            ServiceName = serviceName,
            ChangeDescription = changeDescription,
            ChangeEnvironment = changeEnvironment,
            ChangeOccurredAt = changeOccurredAt
        };
    }
}

/// <summary>Identificador fortemente tipado de IncidentChangeCorrelation.</summary>
public sealed record IncidentChangeCorrelationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static IncidentChangeCorrelationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static IncidentChangeCorrelationId From(Guid id) => new(id);
}
