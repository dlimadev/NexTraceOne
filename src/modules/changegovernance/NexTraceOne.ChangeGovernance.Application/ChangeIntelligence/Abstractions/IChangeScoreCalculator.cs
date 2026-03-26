using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>
/// Serviço de cálculo automático do ChangeIntelligenceScore.
/// Deriva os pesos dos fatores (breaking change, blast radius, ambiente)
/// a partir dos artefactos de domínio já existentes.
///
/// Responsabilidade: tornar o score automático, rastreável e explicável.
/// O cálculo é determinístico — os mesmos inputs produzem sempre o mesmo score.
/// </summary>
public interface IChangeScoreCalculator
{
    /// <summary>
    /// Calcula os pesos mínimos dos fatores a partir dos dados de domínio disponíveis.
    /// Retorna um resultado com pesos explícitos e explicação dos fatores.
    /// </summary>
    ScoreFactors Compute(
        ChangeLevel changeLevel,
        string environment,
        BlastRadiusReport? blastRadius);
}

/// <summary>
/// Resultado do cálculo automático de fatores do ChangeIntelligenceScore.
/// Cada campo explica como o score foi derivado para rastreabilidade.
/// </summary>
public sealed record ScoreFactors(
    decimal BreakingChangeWeight,
    decimal BlastRadiusWeight,
    decimal EnvironmentWeight,
    decimal ComputedScore,
    string BreakingChangeReason,
    string BlastRadiusReason,
    string EnvironmentReason,
    string ScoreSource);
