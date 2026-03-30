using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que regista um evento de deployment de uma versão de contrato num ambiente específico.
/// Liga o contrato ao ambiente de destino permitindo rastreabilidade de mudanças e
/// alimentando o Change Intelligence do NexTraceOne.
/// Cada registo representa uma acção de deployment: success, failure ou rollback.
/// </summary>
public sealed class ContractDeployment : Entity<ContractDeploymentId>
{
    private ContractDeployment() { }

    /// <summary>Identificador da versão de contrato deployada.</summary>
    public ContractVersionId ContractVersionId { get; private set; } = ContractVersionId.New();

    /// <summary>Identificador do ativo de API associado à versão deployada.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Ambiente de destino do deployment (ex: "production", "staging", "development").</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Versão semântica do contrato no momento do deployment.</summary>
    public string SemVer { get; private set; } = string.Empty;

    /// <summary>Estado do deployment (Pending, Success, Failed, Rollback).</summary>
    public ContractDeploymentStatus Status { get; private set; }

    /// <summary>Data/hora UTC em que o deployment foi registado.</summary>
    public DateTimeOffset DeployedAt { get; private set; }

    /// <summary>Identificador do utilizador ou sistema que registou o deployment.</summary>
    public string DeployedBy { get; private set; } = string.Empty;

    /// <summary>Sistema de origem do evento (ex: "github-actions", "jenkins", "azure-devops", "manual").</summary>
    public string SourceSystem { get; private set; } = string.Empty;

    /// <summary>Notas ou observações adicionais sobre o deployment (opcional).</summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Cria um novo registo de deployment de contrato.
    /// Valida os campos obrigatórios e inicializa o estado de rastreabilidade.
    /// </summary>
    public static ContractDeployment Create(
        ContractVersionId contractVersionId,
        Guid apiAssetId,
        string environment,
        string semVer,
        ContractDeploymentStatus status,
        DateTimeOffset deployedAt,
        string deployedBy,
        string sourceSystem,
        string? notes)
    {
        Guard.Against.Null(contractVersionId);
        Guard.Against.Default(apiAssetId);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.NullOrWhiteSpace(semVer);
        Guard.Against.NullOrWhiteSpace(deployedBy);
        Guard.Against.NullOrWhiteSpace(sourceSystem);

        return new ContractDeployment
        {
            Id = ContractDeploymentId.New(),
            ContractVersionId = contractVersionId,
            ApiAssetId = apiAssetId,
            Environment = environment,
            SemVer = semVer,
            Status = status,
            DeployedAt = deployedAt,
            DeployedBy = deployedBy,
            SourceSystem = sourceSystem,
            Notes = notes,
        };
    }

    /// <summary>Actualiza o estado do deployment (ex: Pending → Success após confirmação).</summary>
    public void UpdateStatus(ContractDeploymentStatus status)
    {
        Status = status;
    }
}

/// <summary>Identificador fortemente tipado de ContractDeployment.</summary>
public sealed record ContractDeploymentId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractDeploymentId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractDeploymentId From(Guid id) => new(id);
}
