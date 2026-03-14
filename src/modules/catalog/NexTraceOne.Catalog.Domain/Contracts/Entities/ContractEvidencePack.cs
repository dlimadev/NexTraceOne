using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Enums;
using NexTraceOne.BuildingBlocks.Domain.Primitives;
using NexTraceOne.Contracts.Domain.Enums;

namespace NexTraceOne.Contracts.Domain.Entities;

/// <summary>
/// Entidade que representa o pacote de evidências técnicas gerado para uma mudança de contrato.
/// Agrega todas as informações necessárias para o workflow de aprovação:
/// diff semântico, classificação de breaking change, scorecard, recomendação de versão,
/// regras violadas e avaliação de impacto em consumers.
/// Serve como fonte de verdade auditável para decisões de governança.
/// </summary>
public sealed class ContractEvidencePack : Entity<ContractEvidencePackId>
{
    private ContractEvidencePack() { }

    /// <summary>Identificador da versão de contrato à qual esta evidência está associada.</summary>
    public ContractVersionId ContractVersionId { get; private set; } = ContractVersionId.New();

    /// <summary>Identificador do ativo de API correspondente.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Protocolo do contrato avaliado.</summary>
    public ContractProtocol Protocol { get; private set; }

    /// <summary>Versão semântica da versão de contrato avaliada.</summary>
    public string SemVer { get; private set; } = string.Empty;

    /// <summary>Nível de mudança detectado no diff semântico.</summary>
    public ChangeLevel ChangeLevel { get; private set; }

    /// <summary>Número de breaking changes detectadas.</summary>
    public int BreakingChangeCount { get; private set; }

    /// <summary>Número de mudanças aditivas detectadas.</summary>
    public int AdditiveChangeCount { get; private set; }

    /// <summary>Número de mudanças non-breaking detectadas.</summary>
    public int NonBreakingChangeCount { get; private set; }

    /// <summary>Versão semântica recomendada pelo motor de contratos.</summary>
    public string RecommendedVersion { get; private set; } = string.Empty;

    /// <summary>Score consolidado do scorecard técnico (0.0 a 1.0).</summary>
    public decimal OverallScore { get; private set; }

    /// <summary>Score de risco técnico (0.0 a 1.0).</summary>
    public decimal RiskScore { get; private set; }

    /// <summary>Número de regras violadas detectadas.</summary>
    public int RuleViolationCount { get; private set; }

    /// <summary>Indica se a mudança requer aprovação de workflow.</summary>
    public bool RequiresWorkflowApproval { get; private set; }

    /// <summary>Indica se a mudança requer comunicação formal para consumers.</summary>
    public bool RequiresChangeNotification { get; private set; }

    /// <summary>Resumo executivo da mudança para uso em workflow e dashboards.</summary>
    public string ExecutiveSummary { get; private set; } = string.Empty;

    /// <summary>Resumo técnico detalhado da mudança.</summary>
    public string TechnicalSummary { get; private set; } = string.Empty;

    /// <summary>Lista de consumers potencialmente impactados.</summary>
    public IReadOnlyList<string> ImpactedConsumers { get; private set; } = [];

    /// <summary>Data/hora em que o evidence pack foi gerado.</summary>
    public DateTimeOffset GeneratedAt { get; private set; }

    /// <summary>Usuário ou sistema que gerou o evidence pack.</summary>
    public string GeneratedBy { get; private set; } = string.Empty;

    /// <summary>
    /// Cria um novo pacote de evidências técnicas para uma mudança de contrato.
    /// </summary>
    public static ContractEvidencePack Create(
        ContractVersionId contractVersionId,
        Guid apiAssetId,
        ContractProtocol protocol,
        string semVer,
        ChangeLevel changeLevel,
        int breakingChangeCount,
        int additiveChangeCount,
        int nonBreakingChangeCount,
        string recommendedVersion,
        decimal overallScore,
        decimal riskScore,
        int ruleViolationCount,
        bool requiresWorkflowApproval,
        bool requiresChangeNotification,
        string executiveSummary,
        string technicalSummary,
        IReadOnlyList<string> impactedConsumers,
        DateTimeOffset generatedAt,
        string generatedBy)
    {
        Guard.Against.Null(contractVersionId);
        Guard.Against.Default(apiAssetId);
        Guard.Against.NullOrWhiteSpace(semVer);
        Guard.Against.NullOrWhiteSpace(recommendedVersion);
        Guard.Against.NullOrWhiteSpace(executiveSummary);
        Guard.Against.NullOrWhiteSpace(technicalSummary);
        Guard.Against.NullOrWhiteSpace(generatedBy);
        Guard.Against.Null(impactedConsumers);

        return new ContractEvidencePack
        {
            Id = ContractEvidencePackId.New(),
            ContractVersionId = contractVersionId,
            ApiAssetId = apiAssetId,
            Protocol = protocol,
            SemVer = semVer,
            ChangeLevel = changeLevel,
            BreakingChangeCount = breakingChangeCount,
            AdditiveChangeCount = additiveChangeCount,
            NonBreakingChangeCount = nonBreakingChangeCount,
            RecommendedVersion = recommendedVersion,
            OverallScore = Math.Clamp(overallScore, 0m, 1m),
            RiskScore = Math.Clamp(riskScore, 0m, 1m),
            RuleViolationCount = ruleViolationCount,
            RequiresWorkflowApproval = requiresWorkflowApproval,
            RequiresChangeNotification = requiresChangeNotification,
            ExecutiveSummary = executiveSummary,
            TechnicalSummary = technicalSummary,
            ImpactedConsumers = impactedConsumers,
            GeneratedAt = generatedAt,
            GeneratedBy = generatedBy
        };
    }
}

/// <summary>Identificador fortemente tipado de ContractEvidencePack.</summary>
public sealed record ContractEvidencePackId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractEvidencePackId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractEvidencePackId From(Guid id) => new(id);
}
