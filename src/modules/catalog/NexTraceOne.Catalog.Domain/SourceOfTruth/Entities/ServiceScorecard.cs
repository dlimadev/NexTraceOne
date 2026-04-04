using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.SourceOfTruth.Entities;

/// <summary>
/// Snapshot de maturidade de um serviço, calculado a partir de dados cross-module.
/// Cobre 8 dimensões: ownership, documentação, contratos, SLOs, observabilidade,
/// change governance, runbooks e segurança.
///
/// Diferencial NexTraceOne: scorecard que integra cobertura de contratos + change
/// governance + SLOs reais — visão unificada que concorrentes como OpsLevel/Cortex
/// não oferecem de forma nativa.
/// </summary>
public sealed class ServiceScorecard : Entity<ServiceScorecardId>
{
    private ServiceScorecard() { }

    /// <summary>Nome do serviço avaliado.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Equipa responsável pelo serviço.</summary>
    public string? TeamName { get; private set; }

    /// <summary>Domínio de negócio ao qual o serviço pertence.</summary>
    public string? Domain { get; private set; }

    // ── Dimensões individuais (0.0 – 1.0) ────────────────────────────

    /// <summary>Ownership: equipa definida, tech owner, business owner (peso 10%).</summary>
    public decimal OwnershipScore { get; private set; }
    public string OwnershipJustification { get; private set; } = string.Empty;

    /// <summary>Documentação: description, doc links, knowledge articles (peso 10%).</summary>
    public decimal DocumentationScore { get; private set; }
    public string DocumentationJustification { get; private set; } = string.Empty;

    /// <summary>Contratos: contrato publicado, contract score, breaking changes (peso 15%).</summary>
    public decimal ContractsScore { get; private set; }
    public string ContractsJustification { get; private set; } = string.Empty;

    /// <summary>SLOs: SLOs definidos, error budget, reliability status (peso 15%).</summary>
    public decimal SlosScore { get; private set; }
    public string SlosJustification { get; private set; } = string.Empty;

    /// <summary>Observabilidade: telemetria, métricas, tracing configurado (peso 15%).</summary>
    public decimal ObservabilityScore { get; private set; }
    public string ObservabilityJustification { get; private set; } = string.Empty;

    /// <summary>Change Governance: releases governados, promotion gates, blast radius (peso 15%).</summary>
    public decimal ChangeGovernanceScore { get; private set; }
    public string ChangeGovernanceJustification { get; private set; } = string.Empty;

    /// <summary>Runbooks: documentação operacional, runbooks, notas (peso 10%).</summary>
    public decimal RunbooksScore { get; private set; }
    public string RunbooksJustification { get; private set; } = string.Empty;

    /// <summary>Segurança: security definitions, lifecycle ativo, classificação (peso 10%).</summary>
    public decimal SecurityScore { get; private set; }
    public string SecurityJustification { get; private set; } = string.Empty;

    // ── Score geral ──────────────────────────────────────────────────

    /// <summary>Score geral ponderado (0.0 – 1.0).</summary>
    public decimal OverallScore { get; private set; }

    /// <summary>Classificação de maturidade derivada do OverallScore.</summary>
    public string MaturityLevel { get; private set; } = string.Empty;

    /// <summary>Timestamp de quando o scorecard foi calculado.</summary>
    public DateTimeOffset ComputedAt { get; private set; }

    /// <summary>Cria um novo snapshot de scorecard para um serviço.</summary>
    public static ServiceScorecard Create(
        string serviceName,
        string? teamName,
        string? domain,
        decimal ownershipScore, string ownershipJustification,
        decimal documentationScore, string documentationJustification,
        decimal contractsScore, string contractsJustification,
        decimal slosScore, string slosJustification,
        decimal observabilityScore, string observabilityJustification,
        decimal changeGovernanceScore, string changeGovernanceJustification,
        decimal runbooksScore, string runbooksJustification,
        decimal securityScore, string securityJustification)
    {
        // Pesos oficiais do scorecard de maturidade
        var overall = Math.Round(
            (ownershipScore * 0.10m) +
            (documentationScore * 0.10m) +
            (contractsScore * 0.15m) +
            (slosScore * 0.15m) +
            (observabilityScore * 0.15m) +
            (changeGovernanceScore * 0.15m) +
            (runbooksScore * 0.10m) +
            (securityScore * 0.10m),
            4);

        var maturity = overall switch
        {
            >= 0.85m => "Optimizing",
            >= 0.70m => "Managed",
            >= 0.50m => "Defined",
            >= 0.30m => "Developing",
            _ => "Initial",
        };

        return new ServiceScorecard
        {
            Id = ServiceScorecardId.New(),
            ServiceName = serviceName,
            TeamName = teamName,
            Domain = domain,
            OwnershipScore = ownershipScore,
            OwnershipJustification = ownershipJustification,
            DocumentationScore = documentationScore,
            DocumentationJustification = documentationJustification,
            ContractsScore = contractsScore,
            ContractsJustification = contractsJustification,
            SlosScore = slosScore,
            SlosJustification = slosJustification,
            ObservabilityScore = observabilityScore,
            ObservabilityJustification = observabilityJustification,
            ChangeGovernanceScore = changeGovernanceScore,
            ChangeGovernanceJustification = changeGovernanceJustification,
            RunbooksScore = runbooksScore,
            RunbooksJustification = runbooksJustification,
            SecurityScore = securityScore,
            SecurityJustification = securityJustification,
            OverallScore = overall,
            MaturityLevel = maturity,
            ComputedAt = DateTimeOffset.UtcNow,
        };
    }
}

/// <summary>Identificador tipado para ServiceScorecard.</summary>
public sealed record ServiceScorecardId(Guid Value) : TypedIdBase(Value)
{
    public static ServiceScorecardId New() => new(Guid.NewGuid());

    public static ServiceScorecardId From(Guid id) => new(id);
}
