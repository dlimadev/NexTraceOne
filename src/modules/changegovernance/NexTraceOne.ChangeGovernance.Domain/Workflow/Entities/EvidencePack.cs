using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

/// <summary>
/// Aggregate Root que representa o pacote de evidências consolidado de um workflow.
/// Agrega scores de risco, diff de contrato, histórico de aprovação e integridade hash
/// para formar o dossiê completo de auditoria de uma mudança.
/// </summary>
public sealed class EvidencePack : AggregateRoot<EvidencePackId>
{
    private EvidencePack() { }

    /// <summary>Identificador da instância de workflow à qual este evidence pack pertence.</summary>
    public WorkflowInstanceId WorkflowInstanceId { get; private set; } = null!;

    /// <summary>Identificador da release associada (módulo ChangeIntelligence).</summary>
    public Guid ReleaseId { get; private set; }

    /// <summary>Resumo textual do diff de contrato.</summary>
    public string? ContractDiffSummary { get; private set; }

    /// <summary>Score de blast radius normalizado (0.0–1.0).</summary>
    public decimal? BlastRadiusScore { get; private set; }

    /// <summary>Score de linting Spectral normalizado (0.0–1.0).</summary>
    public decimal? SpectralScore { get; private set; }

    /// <summary>Score de change intelligence normalizado (0.0–1.0).</summary>
    public decimal? ChangeIntelligenceScore { get; private set; }

    /// <summary>Histórico de aprovações serializado em JSON.</summary>
    public string? ApprovalHistory { get; private set; }

    /// <summary>Hash SHA-256 do contrato para verificação de integridade.</summary>
    public string? ContractHash { get; private set; }

    // ── Campos CI/CD (P5.4) ──────────────────────────────────────────────────

    /// <summary>Sistema de CI/CD de origem (ex: "github-actions", "jenkins", "azure-devops").</summary>
    public string? PipelineSource { get; private set; }

    /// <summary>Identificador externo do build/run de CI/CD (vindo do ExternalMarker.ExternalId).</summary>
    public string? BuildId { get; private set; }

    /// <summary>SHA do commit git que originou o pipeline (vindo da Release).</summary>
    public string? CommitSha { get; private set; }

    /// <summary>Resultado consolidado dos checks de CI: "passed", "failed", "partial", "unknown".</summary>
    public string? CiChecksResult { get; private set; }

    // ── Fim campos CI/CD ──────────────────────────────────────────────────────

    /// <summary>Percentual de completude do evidence pack (0–100).</summary>
    public decimal CompletenessPercentage { get; private set; }

    /// <summary>Data/hora UTC em que o evidence pack foi gerado.</summary>
    public DateTimeOffset GeneratedAt { get; private set; }

    /// <summary>
    /// Cria um novo evidence pack vinculado a uma instância de workflow e release.
    /// </summary>
    public static EvidencePack Create(
        WorkflowInstanceId workflowInstanceId,
        Guid releaseId,
        DateTimeOffset generatedAt)
    {
        Guard.Against.Null(workflowInstanceId);
        Guard.Against.Default(releaseId);

        var pack = new EvidencePack
        {
            Id = EvidencePackId.New(),
            WorkflowInstanceId = workflowInstanceId,
            ReleaseId = releaseId,
            CompletenessPercentage = 0m,
            GeneratedAt = generatedAt
        };

        pack.RecalculateCompleteness();
        return pack;
    }

    /// <summary>
    /// Atualiza os scores de risco do evidence pack.
    /// </summary>
    public void UpdateScores(decimal? blastRadius, decimal? spectral, decimal? changeIntelligence)
    {
        BlastRadiusScore = blastRadius;
        SpectralScore = spectral;
        ChangeIntelligenceScore = changeIntelligence;
        RecalculateCompleteness();
    }

    /// <summary>Define o resumo do diff de contrato.</summary>
    public void SetContractDiff(string summary)
    {
        Guard.Against.NullOrWhiteSpace(summary);
        ContractDiffSummary = summary;
        RecalculateCompleteness();
    }

    /// <summary>Define o hash de integridade do contrato.</summary>
    public void SetContractHash(string hash)
    {
        Guard.Against.NullOrWhiteSpace(hash);
        ContractHash = hash;
        RecalculateCompleteness();
    }

    /// <summary>Define o histórico de aprovações serializado em JSON.</summary>
    public void SetApprovalHistory(string historyJson)
    {
        Guard.Against.NullOrWhiteSpace(historyJson);
        ApprovalHistory = historyJson;
        RecalculateCompleteness();
    }

    /// <summary>
    /// Anexa evidências automáticas provenientes do pipeline CI/CD ao evidence pack.
    /// Chamado automaticamente quando um evento de deploy (ExternalMarker) é recebido.
    /// </summary>
    public void AttachCiCdEvidence(
        string pipelineSource,
        string? buildId,
        string? commitSha,
        string? ciChecksResult)
    {
        Guard.Against.NullOrWhiteSpace(pipelineSource);

        PipelineSource = pipelineSource;
        BuildId = buildId;
        CommitSha = commitSha;
        CiChecksResult = ciChecksResult ?? "unknown";
        RecalculateCompleteness();
    }

    /// <summary>
    /// Recalcula o percentual de completude com base nos campos preenchidos.
    /// Campos considerados: ContractDiffSummary, BlastRadiusScore, SpectralScore,
    /// ChangeIntelligenceScore, ApprovalHistory e PipelineSource (6 campos no total).
    /// Cada campo preenchido contribui igualmente para o percentual total.
    /// </summary>
    public void RecalculateCompleteness()
    {
        var filledFields = 0;

        if (!string.IsNullOrWhiteSpace(ContractDiffSummary)) filledFields++;
        if (BlastRadiusScore.HasValue) filledFields++;
        if (SpectralScore.HasValue) filledFields++;
        if (ChangeIntelligenceScore.HasValue) filledFields++;
        if (!string.IsNullOrWhiteSpace(ApprovalHistory)) filledFields++;
        if (!string.IsNullOrWhiteSpace(PipelineSource)) filledFields++;

        const int totalFields = 6;
        CompletenessPercentage = Math.Round((decimal)filledFields / totalFields * 100m, 2);
    }
}

/// <summary>Identificador fortemente tipado de EvidencePack.</summary>
public sealed record EvidencePackId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static EvidencePackId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static EvidencePackId From(Guid id) => new(id);
}
