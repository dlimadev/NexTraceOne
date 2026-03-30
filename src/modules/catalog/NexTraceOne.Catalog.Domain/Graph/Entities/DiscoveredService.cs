using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Domain.Graph.Entities;

/// <summary>
/// Serviço descoberto automaticamente a partir de telemetria (traces OTel, logs).
/// Representa um serviço observado em runtime que pode ainda não estar registado
/// no catálogo oficial do NexTraceOne.
///
/// Fluxo: Pending → (Match | Ignore | Register).
/// Quando Matched ou Registered, liga-se ao ServiceAssetId correspondente.
/// </summary>
public sealed class DiscoveredService : Entity<DiscoveredServiceId>
{
    private DiscoveredService() { }

    // ── Identidade da descoberta ───────────────────────────────────────

    /// <summary>Nome do serviço conforme reportado pela telemetria (service.name OTel).</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Namespace do serviço conforme reportado (service.namespace OTel, opcional).</summary>
    public string ServiceNamespace { get; private set; } = string.Empty;

    /// <summary>Ambiente onde o serviço foi observado.</summary>
    public string Environment { get; private set; } = string.Empty;

    // ── Observação ─────────────────────────────────────────────────────

    /// <summary>Data/hora UTC da primeira observação deste serviço.</summary>
    public DateTimeOffset FirstSeenAt { get; private set; }

    /// <summary>Data/hora UTC da última observação deste serviço.</summary>
    public DateTimeOffset LastSeenAt { get; private set; }

    /// <summary>Quantidade total de traces observados para este serviço.</summary>
    public long TraceCount { get; private set; }

    /// <summary>Quantidade de endpoints/operações distintas observadas.</summary>
    public int EndpointCount { get; private set; }

    // ── Estado e triagem ──────────────────────────────────────────────

    /// <summary>Estado de processamento da descoberta.</summary>
    public DiscoveryStatus Status { get; private set; } = DiscoveryStatus.Pending;

    /// <summary>Id do ServiceAsset associado (quando Matched ou Registered).</summary>
    public Guid? MatchedServiceAssetId { get; private set; }

    /// <summary>Id do DiscoveryRun que criou este registo.</summary>
    public Guid DiscoveryRunId { get; private set; }

    /// <summary>Motivo de ignore (quando Status = Ignored).</summary>
    public string? IgnoreReason { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────

    /// <summary>Cria um novo serviço descoberto a partir de dados de telemetria.</summary>
    public static DiscoveredService Create(
        string serviceName,
        string serviceNamespace,
        string environment,
        DateTimeOffset firstSeenAt,
        DateTimeOffset lastSeenAt,
        long traceCount,
        int endpointCount,
        Guid discoveryRunId)
    {
        return new DiscoveredService
        {
            Id = DiscoveredServiceId.New(),
            ServiceName = Guard.Against.NullOrWhiteSpace(serviceName),
            ServiceNamespace = serviceNamespace ?? string.Empty,
            Environment = Guard.Against.NullOrWhiteSpace(environment),
            FirstSeenAt = firstSeenAt,
            LastSeenAt = lastSeenAt,
            TraceCount = traceCount,
            EndpointCount = endpointCount,
            Status = DiscoveryStatus.Pending,
            DiscoveryRunId = discoveryRunId
        };
    }

    // ── Mutations controladas ─────────────────────────────────────────

    /// <summary>Atualiza contadores e última observação com dados de nova execução.</summary>
    public void UpdateObservation(DateTimeOffset lastSeenAt, long traceCount, int endpointCount)
    {
        if (lastSeenAt > LastSeenAt)
        {
            LastSeenAt = lastSeenAt;
        }

        TraceCount = traceCount;
        EndpointCount = endpointCount;
    }

    /// <summary>Associa este serviço descoberto a um ServiceAsset existente.</summary>
    public void MatchToService(Guid serviceAssetId)
    {
        Guard.Against.Default(serviceAssetId);
        Status = DiscoveryStatus.Matched;
        MatchedServiceAssetId = serviceAssetId;
        IgnoreReason = null;
    }

    /// <summary>Marca serviço como ignorado com motivo obrigatório.</summary>
    public void Ignore(string reason)
    {
        Guard.Against.NullOrWhiteSpace(reason);
        Status = DiscoveryStatus.Ignored;
        IgnoreReason = reason;
        MatchedServiceAssetId = null;
    }

    /// <summary>Marca serviço como registado no catálogo.</summary>
    public void MarkAsRegistered(Guid serviceAssetId)
    {
        Guard.Against.Default(serviceAssetId);
        Status = DiscoveryStatus.Registered;
        MatchedServiceAssetId = serviceAssetId;
        IgnoreReason = null;
    }

    /// <summary>Repõe o estado para Pending (desfaz match ou ignore).</summary>
    public void ResetToPending()
    {
        Status = DiscoveryStatus.Pending;
        MatchedServiceAssetId = null;
        IgnoreReason = null;
    }
}

/// <summary>Identificador fortemente tipado de DiscoveredService.</summary>
public sealed record DiscoveredServiceId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static DiscoveredServiceId New() => new(Guid.NewGuid());

    /// <summary>Cria a partir de Guid existente.</summary>
    public static DiscoveredServiceId From(Guid value) => new(value);
}
