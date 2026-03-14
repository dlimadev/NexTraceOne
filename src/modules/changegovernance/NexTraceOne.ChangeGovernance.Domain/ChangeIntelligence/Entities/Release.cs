using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeIntelligence.Domain.Errors;

namespace NexTraceOne.ChangeIntelligence.Domain.Entities;

/// <summary>
/// Aggregate Root que representa uma release de um serviço/API no pipeline de CI/CD.
/// Gerencia o ciclo de vida do deployment, classificação de mudança e score de risco.
/// </summary>
public sealed class Release : AggregateRoot<ReleaseId>
{
    private Release() { }

    /// <summary>Identificador do ativo de API correspondente no módulo Catalog Graph.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Nome do serviço que gerou a release.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Versão semver da release.</summary>
    public string Version { get; private set; } = string.Empty;

    /// <summary>Ambiente de destino do deployment (dev, staging, prod).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Referência do pipeline de CI/CD (URL ou nome).</summary>
    public string PipelineSource { get; private set; } = string.Empty;

    /// <summary>SHA do commit git associado à release.</summary>
    public string CommitSha { get; private set; } = string.Empty;

    /// <summary>Nível de mudança classificado para esta release.</summary>
    public ChangeLevel ChangeLevel { get; private set; } = ChangeLevel.Operational;

    /// <summary>Status atual do deployment.</summary>
    public DeploymentStatus Status { get; private set; } = DeploymentStatus.Pending;

    /// <summary>Score de risco normalizado entre 0.0 e 1.0.</summary>
    public decimal ChangeScore { get; private set; }

    /// <summary>Referência opcional ao work item (ticket, issue) relacionado.</summary>
    public string? WorkItemReference { get; private set; }

    /// <summary>Se esta release é um rollback, identificador da release original.</summary>
    public ReleaseId? RolledBackFromReleaseId { get; private set; }

    /// <summary>Data/hora UTC em que a release foi criada.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Cria uma nova release a partir de um evento de deployment recebido do CI/CD.
    /// Validações de negócio mais profundas são feitas no command handler.
    /// </summary>
    public static Release Create(
        Guid apiAssetId,
        string serviceName,
        string version,
        string environment,
        string pipelineSource,
        string commitSha,
        DateTimeOffset createdAt)
    {
        Guard.Against.Default(apiAssetId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(version);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.NullOrWhiteSpace(pipelineSource);
        Guard.Against.NullOrWhiteSpace(commitSha);

        return new Release
        {
            Id = ReleaseId.New(),
            ApiAssetId = apiAssetId,
            ServiceName = serviceName,
            Version = version,
            Environment = environment,
            PipelineSource = pipelineSource,
            CommitSha = commitSha,
            Status = DeploymentStatus.Pending,
            ChangeLevel = ChangeLevel.Operational,
            ChangeScore = 0m,
            CreatedAt = createdAt
        };
    }

    /// <summary>Classifica o nível de mudança desta release.</summary>
    public void Classify(ChangeLevel changeLevel)
    {
        ChangeLevel = changeLevel;
    }

    /// <summary>
    /// Atualiza o status do deployment desta release.
    /// Retorna falha se a transição de status for inválida.
    /// </summary>
    public Result<Unit> UpdateStatus(DeploymentStatus status)
    {
        if (!IsValidTransition(Status, status))
            return ChangeIntelligenceErrors.InvalidStatusTransition(Status.ToString(), status.ToString());

        Status = status;
        return Unit.Value;
    }

    /// <summary>
    /// Marca esta release como um rollback de outra release.
    /// Retorna falha se esta release já estiver marcada como rollback.
    /// </summary>
    public Result<Unit> RegisterRollback(ReleaseId originalReleaseId)
    {
        Guard.Against.Null(originalReleaseId);

        if (RolledBackFromReleaseId is not null)
            return ChangeIntelligenceErrors.AlreadyRollback();

        RolledBackFromReleaseId = originalReleaseId;
        return UpdateStatus(DeploymentStatus.RolledBack);
    }

    /// <summary>
    /// Define o score de risco desta release.
    /// Retorna falha se o score estiver fora do intervalo 0.0–1.0.
    /// </summary>
    public Result<Unit> SetChangeScore(decimal score)
    {
        if (score < 0m || score > 1m)
            return ChangeIntelligenceErrors.InvalidChangeScore(score);

        ChangeScore = score;
        return Unit.Value;
    }

    /// <summary>Associa uma referência de work item (ticket/issue) a esta release.</summary>
    public void AttachWorkItem(string workItemRef)
    {
        Guard.Against.NullOrWhiteSpace(workItemRef);
        WorkItemReference = workItemRef;
    }

    /// <summary>
    /// Verifica se a transição de status é válida segundo o ciclo de vida do deployment.
    /// Transições permitidas: Pending→Running→(Succeeded|Failed)→RolledBack.
    /// </summary>
    private static bool IsValidTransition(DeploymentStatus from, DeploymentStatus to) =>
        (from, to) switch
        {
            (DeploymentStatus.Pending, DeploymentStatus.Running) => true,
            (DeploymentStatus.Running, DeploymentStatus.Succeeded) => true,
            (DeploymentStatus.Running, DeploymentStatus.Failed) => true,
            (DeploymentStatus.Succeeded, DeploymentStatus.RolledBack) => true,
            (DeploymentStatus.Failed, DeploymentStatus.RolledBack) => true,
            _ => false
        };
}

/// <summary>Identificador fortemente tipado de Release.</summary>
public sealed record ReleaseId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ReleaseId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ReleaseId From(Guid id) => new(id);
}
