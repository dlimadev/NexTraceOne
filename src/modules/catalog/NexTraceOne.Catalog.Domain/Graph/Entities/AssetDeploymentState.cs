using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Domain.Graph.Entities;

/// <summary>
/// Estado de deployment de um ServiceAsset num ambiente específico.
/// Representa "serviço X está na versão Y no ambiente Z" — a ponte entre
/// o catálogo estático e a realidade do runtime.
///
/// Semântica de upsert: uma linha por (ServiceAssetId, Environment).
/// Novos deployments chamam <see cref="UpdateDeployment"/> em vez de criar nova linha.
/// </summary>
public sealed class AssetDeploymentState : AuditableEntity<AssetDeploymentStateId>
{
    private AssetDeploymentState() { }

    /// <summary>Serviço ao qual este estado pertence.</summary>
    public ServiceAssetId ServiceAssetId { get; private set; } = null!;

    /// <summary>Tenant proprietário deste registo.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Ambiente de execução (production, staging, development, etc.).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Tag ou versão da imagem deployada (ex: v2.3.1, sha-abc1234).</summary>
    public string ImageTag { get; private set; } = string.Empty;

    /// <summary>Nome ou identificador do release que originou este deployment.</summary>
    public string ReleaseName { get; private set; } = string.Empty;

    /// <summary>Estado actual do runtime para este serviço/ambiente.</summary>
    public RuntimeStatus RuntimeStatus { get; private set; } = RuntimeStatus.Unknown;

    /// <summary>Último sinal de vida recebido (heartbeat OTel ou health check).</summary>
    public DateTimeOffset LastHeartbeatAt { get; private set; }

    /// <summary>Data/hora UTC em que o deployment foi registado.</summary>
    public DateTimeOffset DeployedAt { get; private set; }

    /// <summary>
    /// Regista o primeiro deployment de um serviço num ambiente.
    /// </summary>
    public static AssetDeploymentState Record(
        ServiceAssetId serviceAssetId,
        Guid tenantId,
        string environment,
        string imageTag,
        string releaseName,
        DateTimeOffset deployedAt)
        => new()
        {
            Id = AssetDeploymentStateId.New(),
            ServiceAssetId = Guard.Against.Null(serviceAssetId),
            TenantId = tenantId,
            Environment = Guard.Against.NullOrWhiteSpace(environment),
            ImageTag = imageTag ?? string.Empty,
            ReleaseName = releaseName ?? string.Empty,
            RuntimeStatus = RuntimeStatus.Deploying,
            LastHeartbeatAt = deployedAt,
            DeployedAt = deployedAt
        };

    /// <summary>
    /// Actualiza o deployment existente com nova versão/release.
    /// Deve ser chamado quando um novo deployment ocorre num ambiente já rastreado.
    /// </summary>
    public void UpdateDeployment(string imageTag, string releaseName, DateTimeOffset deployedAt)
    {
        ImageTag = imageTag ?? string.Empty;
        ReleaseName = releaseName ?? string.Empty;
        RuntimeStatus = RuntimeStatus.Deploying;
        DeployedAt = deployedAt;
        LastHeartbeatAt = deployedAt;
    }

    /// <summary>
    /// Actualiza o estado de runtime a partir de sinais de telemetria ou health checks.
    /// </summary>
    public void UpdateStatus(RuntimeStatus status, DateTimeOffset observedAt)
    {
        RuntimeStatus = status;
        LastHeartbeatAt = observedAt;
    }
}

/// <summary>Identificador fortemente tipado de AssetDeploymentState.</summary>
public sealed record AssetDeploymentStateId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AssetDeploymentStateId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AssetDeploymentStateId From(Guid id) => new(id);
}
