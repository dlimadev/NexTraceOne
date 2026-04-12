using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que regista cada execução de verificação de contrato proveniente de CI/CD.
/// É um registo imutável (log) que associa a especificação submetida ao resultado
/// da verificação, incluindo diff semântico, violações de compliance e metadados
/// da pipeline de origem. Alimenta o Change Intelligence e a governança contratual.
/// </summary>
public sealed class ContractVerification : Entity<ContractVerificationId>
{
    private ContractVerification() { }

    /// <summary>Identificador do tenant para isolamento multi-tenant.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Identificador do ativo de API verificado.</summary>
    public string ApiAssetId { get; private set; } = string.Empty;

    /// <summary>Nome do serviço associado ao contrato verificado.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Identificador da versão de contrato verificada (nulo se nenhum contrato registado).</summary>
    public Guid? ContractVersionId { get; private set; }

    /// <summary>Hash SHA-256 do conteúdo da especificação submetida.</summary>
    public string SpecContentHash { get; private set; } = string.Empty;

    /// <summary>Estado do resultado da verificação.</summary>
    public VerificationStatus Status { get; private set; }

    /// <summary>Número de breaking changes detetadas.</summary>
    public int BreakingChangesCount { get; private set; }

    /// <summary>Número de alterações não disruptivas detetadas.</summary>
    public int NonBreakingChangesCount { get; private set; }

    /// <summary>Número de adições detetadas (novos endpoints, campos, etc.).</summary>
    public int AdditiveChangesCount { get; private set; }

    /// <summary>Detalhes do diff semântico em formato JSON (JSONB).</summary>
    public string DiffDetails { get; private set; } = string.Empty;

    /// <summary>Violações de compliance detetadas em formato JSON (JSONB).</summary>
    public string ComplianceViolations { get; private set; } = string.Empty;

    /// <summary>Sistema de origem da verificação (ex: "github-actions", "jenkins").</summary>
    public string SourceSystem { get; private set; } = string.Empty;

    /// <summary>Branch de origem da verificação (opcional).</summary>
    public string? SourceBranch { get; private set; }

    /// <summary>SHA do commit associado à verificação (opcional).</summary>
    public string? CommitSha { get; private set; }

    /// <summary>Identificador da pipeline de origem (opcional).</summary>
    public string? PipelineId { get; private set; }

    /// <summary>Nome do ambiente onde a verificação decorreu (opcional).</summary>
    public string? EnvironmentName { get; private set; }

    /// <summary>Momento em que a verificação foi executada.</summary>
    public DateTimeOffset VerifiedAt { get; private set; }

    /// <summary>Momento de criação do registo.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Identificador do utilizador ou sistema que criou o registo.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria um novo registo de verificação de contrato.
    /// Valida os campos obrigatórios e inicializa o estado de rastreabilidade.
    /// </summary>
    public static ContractVerification Create(
        string tenantId,
        string apiAssetId,
        string serviceName,
        Guid? contractVersionId,
        string specContentHash,
        VerificationStatus status,
        int breakingChangesCount,
        int nonBreakingChangesCount,
        int additiveChangesCount,
        string diffDetails,
        string complianceViolations,
        string sourceSystem,
        string? sourceBranch,
        string? commitSha,
        string? pipelineId,
        string? environmentName,
        DateTimeOffset verifiedAt,
        DateTimeOffset createdAt,
        string createdBy)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(apiAssetId);
        Guard.Against.StringTooLong(apiAssetId, 200);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.StringTooLong(serviceName, 300);
        Guard.Against.NullOrWhiteSpace(specContentHash);
        Guard.Against.StringTooLong(specContentHash, 128);
        Guard.Against.EnumOutOfRange(status);
        Guard.Against.Negative(breakingChangesCount);
        Guard.Against.Negative(nonBreakingChangesCount);
        Guard.Against.Negative(additiveChangesCount);
        Guard.Against.NullOrWhiteSpace(sourceSystem);
        Guard.Against.StringTooLong(sourceSystem, 100);
        Guard.Against.NullOrWhiteSpace(createdBy);
        Guard.Against.StringTooLong(createdBy, 200);

        if (sourceBranch is not null)
            Guard.Against.StringTooLong(sourceBranch, 500);

        if (commitSha is not null)
            Guard.Against.StringTooLong(commitSha, 100);

        if (pipelineId is not null)
            Guard.Against.StringTooLong(pipelineId, 200);

        if (environmentName is not null)
            Guard.Against.StringTooLong(environmentName, 200);

        return new ContractVerification
        {
            Id = ContractVerificationId.New(),
            TenantId = tenantId,
            ApiAssetId = apiAssetId,
            ServiceName = serviceName,
            ContractVersionId = contractVersionId,
            SpecContentHash = specContentHash,
            Status = status,
            BreakingChangesCount = breakingChangesCount,
            NonBreakingChangesCount = nonBreakingChangesCount,
            AdditiveChangesCount = additiveChangesCount,
            DiffDetails = diffDetails,
            ComplianceViolations = complianceViolations,
            SourceSystem = sourceSystem,
            SourceBranch = sourceBranch,
            CommitSha = commitSha,
            PipelineId = pipelineId,
            EnvironmentName = environmentName,
            VerifiedAt = verifiedAt,
            CreatedAt = createdAt,
            CreatedBy = createdBy,
        };
    }
}
