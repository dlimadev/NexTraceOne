using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.RulesetGovernance.Domain.Entities;

/// <summary>
/// Entidade que armazena o resultado de uma execução de linting sobre uma release/API.
/// Contém score de conformidade, total de findings e a lista detalhada de findings.
/// </summary>
public sealed class LintResult : AuditableEntity<LintResultId>
{
    private readonly List<Finding> _findings = [];

    private LintResult() { }

    /// <summary>Identificador do ruleset utilizado na execução.</summary>
    public RulesetId RulesetId { get; private set; } = null!;

    /// <summary>Identificador da release avaliada.</summary>
    public Guid ReleaseId { get; private set; }

    /// <summary>Identificador do ativo de API avaliado.</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Score de conformidade entre 0 e 100.</summary>
    public decimal Score { get; private set; }

    /// <summary>Total de findings encontrados.</summary>
    public int TotalFindings { get; private set; }

    /// <summary>Lista detalhada de findings da execução de linting.</summary>
    public IReadOnlyList<Finding> Findings => _findings.AsReadOnly();

    /// <summary>Data/hora UTC em que a execução de linting foi realizada.</summary>
    public DateTimeOffset ExecutedAt { get; private set; }

    /// <summary>
    /// Cria um novo resultado de linting com os findings informados.
    /// </summary>
    public static LintResult Create(
        RulesetId rulesetId,
        Guid releaseId,
        Guid apiAssetId,
        decimal score,
        IReadOnlyList<Finding> findings,
        DateTimeOffset executedAt)
    {
        Guard.Against.Null(rulesetId);
        Guard.Against.Default(releaseId);
        Guard.Against.Default(apiAssetId);

        var result = new LintResult
        {
            Id = LintResultId.New(),
            RulesetId = rulesetId,
            ReleaseId = releaseId,
            ApiAssetId = apiAssetId,
            Score = Math.Clamp(score, 0m, 100m),
            TotalFindings = findings.Count,
            ExecutedAt = executedAt
        };

        result._findings.AddRange(findings);
        return result;
    }
}

/// <summary>Identificador fortemente tipado de LintResult.</summary>
public sealed record LintResultId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static LintResultId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static LintResultId From(Guid id) => new(id);
}
