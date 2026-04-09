using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade TeamHealthSnapshot.
/// Garante que nunca seja confundido com outro tipo de Guid no sistema.
/// </summary>
public sealed record TeamHealthSnapshotId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Snapshot de saúde de uma equipa, calculado a partir de 7 dimensões operacionais.
/// Cada dimensão tem um score de 0 a 100 e o OverallScore é a média aritmética.
/// Suporta recomputação para refletir mudanças nas métricas ao longo do tempo.
/// </summary>
public sealed class TeamHealthSnapshot : Entity<TeamHealthSnapshotId>
{
    /// <summary>Número de dimensões de saúde avaliadas.</summary>
    private const int DimensionCount = 7;

    /// <summary>Identificador da equipa avaliada (FK lógica).</summary>
    public Guid TeamId { get; private init; }

    /// <summary>Nome desnormalizado da equipa para exibição sem join.</summary>
    public string TeamName { get; private set; } = string.Empty;

    /// <summary>Score composto (média das 7 dimensões), 0-100.</summary>
    public int OverallScore { get; private set; }

    /// <summary>Score da dimensão ServiceCount, 0-100.</summary>
    public int ServiceCountScore { get; private set; }

    /// <summary>Score da dimensão ContractHealth, 0-100.</summary>
    public int ContractHealthScore { get; private set; }

    /// <summary>Score da dimensão IncidentFrequency, 0-100.</summary>
    public int IncidentFrequencyScore { get; private set; }

    /// <summary>Score da dimensão Mean Time To Resolve, 0-100.</summary>
    public int MttrScore { get; private set; }

    /// <summary>Score da dimensão TechDebt, 0-100.</summary>
    public int TechDebtScore { get; private set; }

    /// <summary>Score da dimensão DocumentationCoverage, 0-100.</summary>
    public int DocCoverageScore { get; private set; }

    /// <summary>Score da dimensão PolicyCompliance, 0-100.</summary>
    public int PolicyComplianceScore { get; private set; }

    /// <summary>Detalhes adicionais em formato JSON (JSONB no PostgreSQL).</summary>
    public string? DimensionDetails { get; private set; }

    /// <summary>Data/hora UTC da avaliação.</summary>
    public DateTimeOffset AssessedAt { get; private set; }

    /// <summary>Identificador do tenant proprietário (nullable para multi-tenant).</summary>
    public string? TenantId { get; private init; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Construtor privado para EF Core e serialização.</summary>
    private TeamHealthSnapshot() { }

    /// <summary>
    /// Cria um novo snapshot de saúde para uma equipa.
    /// O OverallScore é calculado como a média aritmética das 7 dimensões.
    /// </summary>
    /// <param name="teamId">Identificador da equipa.</param>
    /// <param name="teamName">Nome da equipa (máx. 200 caracteres).</param>
    /// <param name="serviceCountScore">Score da dimensão ServiceCount (0-100).</param>
    /// <param name="contractHealthScore">Score da dimensão ContractHealth (0-100).</param>
    /// <param name="incidentFrequencyScore">Score da dimensão IncidentFrequency (0-100).</param>
    /// <param name="mttrScore">Score da dimensão MTTR (0-100).</param>
    /// <param name="techDebtScore">Score da dimensão TechDebt (0-100).</param>
    /// <param name="docCoverageScore">Score da dimensão DocumentationCoverage (0-100).</param>
    /// <param name="policyComplianceScore">Score da dimensão PolicyCompliance (0-100).</param>
    /// <param name="dimensionDetails">Detalhes adicionais em JSON (opcional).</param>
    /// <param name="tenantId">Identificador do tenant (opcional).</param>
    /// <param name="now">Data/hora UTC da avaliação.</param>
    /// <returns>Nova instância válida de TeamHealthSnapshot.</returns>
    public static TeamHealthSnapshot Compute(
        Guid teamId,
        string teamName,
        int serviceCountScore,
        int contractHealthScore,
        int incidentFrequencyScore,
        int mttrScore,
        int techDebtScore,
        int docCoverageScore,
        int policyComplianceScore,
        string? dimensionDetails,
        string? tenantId,
        DateTimeOffset now)
    {
        Guard.Against.Default(teamId, nameof(teamId));
        Guard.Against.NullOrWhiteSpace(teamName, nameof(teamName));
        Guard.Against.StringTooLong(teamName, 200, nameof(teamName));

        ValidateScore(serviceCountScore, nameof(serviceCountScore));
        ValidateScore(contractHealthScore, nameof(contractHealthScore));
        ValidateScore(incidentFrequencyScore, nameof(incidentFrequencyScore));
        ValidateScore(mttrScore, nameof(mttrScore));
        ValidateScore(techDebtScore, nameof(techDebtScore));
        ValidateScore(docCoverageScore, nameof(docCoverageScore));
        ValidateScore(policyComplianceScore, nameof(policyComplianceScore));

        var snapshot = new TeamHealthSnapshot
        {
            Id = new TeamHealthSnapshotId(Guid.NewGuid()),
            TeamId = teamId,
            TeamName = teamName.Trim(),
            ServiceCountScore = serviceCountScore,
            ContractHealthScore = contractHealthScore,
            IncidentFrequencyScore = incidentFrequencyScore,
            MttrScore = mttrScore,
            TechDebtScore = techDebtScore,
            DocCoverageScore = docCoverageScore,
            PolicyComplianceScore = policyComplianceScore,
            DimensionDetails = dimensionDetails,
            TenantId = tenantId?.Trim(),
            AssessedAt = now
        };

        snapshot.OverallScore = snapshot.ComputeOverall();

        return snapshot;
    }

    /// <summary>
    /// Recomputa o snapshot com novos scores.
    /// Atualiza todas as dimensões e recalcula o OverallScore.
    /// </summary>
    public void Recompute(
        int serviceCountScore,
        int contractHealthScore,
        int incidentFrequencyScore,
        int mttrScore,
        int techDebtScore,
        int docCoverageScore,
        int policyComplianceScore,
        string? dimensionDetails,
        DateTimeOffset now)
    {
        ValidateScore(serviceCountScore, nameof(serviceCountScore));
        ValidateScore(contractHealthScore, nameof(contractHealthScore));
        ValidateScore(incidentFrequencyScore, nameof(incidentFrequencyScore));
        ValidateScore(mttrScore, nameof(mttrScore));
        ValidateScore(techDebtScore, nameof(techDebtScore));
        ValidateScore(docCoverageScore, nameof(docCoverageScore));
        ValidateScore(policyComplianceScore, nameof(policyComplianceScore));

        ServiceCountScore = serviceCountScore;
        ContractHealthScore = contractHealthScore;
        IncidentFrequencyScore = incidentFrequencyScore;
        MttrScore = mttrScore;
        TechDebtScore = techDebtScore;
        DocCoverageScore = docCoverageScore;
        PolicyComplianceScore = policyComplianceScore;
        DimensionDetails = dimensionDetails;
        AssessedAt = now;

        OverallScore = ComputeOverall();
    }

    /// <summary>
    /// Calcula o score composto como média aritmética das 7 dimensões (arredondamento inteiro).
    /// </summary>
    private int ComputeOverall()
    {
        var sum = ServiceCountScore
                  + ContractHealthScore
                  + IncidentFrequencyScore
                  + MttrScore
                  + TechDebtScore
                  + DocCoverageScore
                  + PolicyComplianceScore;

        return (int)Math.Round(sum / (double)DimensionCount, MidpointRounding.AwayFromZero);
    }

    private static void ValidateScore(int score, string paramName)
    {
        if (score < 0 || score > 100)
            throw new ArgumentOutOfRangeException(paramName, score, "Score must be between 0 and 100.");
    }
}
