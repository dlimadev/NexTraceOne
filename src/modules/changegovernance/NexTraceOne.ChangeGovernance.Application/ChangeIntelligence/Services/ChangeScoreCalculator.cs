using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Services;

/// <summary>
/// Implementação determinística do cálculo automático de ChangeIntelligenceScore.
///
/// MODELO DE PESOS (P5.3):
///
/// BreakingChangeWeight — derivado do ChangeLevel:
///   Operational  = 0.0  (sem mudança de contrato)
///   NonBreaking  = 0.1  (patch — risco mínimo)
///   Additive     = 0.4  (minor — risco moderado)
///   Breaking     = 1.0  (major — risco máximo)
///   Publication  = 0.1  (publicação sem nova versão)
///
/// BlastRadiusWeight — derivado do TotalAffectedConsumers:
///   0 consumidores  = 0.0
///   1–5             = 0.3
///   6–20            = 0.6
///   21+             = 1.0
///   (sem relatório) = 0.0 (peso zero — blast radius não calculado ainda)
///
/// EnvironmentWeight — derivado do nome do ambiente:
///   production / prod     = 1.0 (maior risco)
///   staging / pre-prod    = 0.6
///   development / dev     = 0.2
///   outros                = 0.4
///
/// Score final = (BreakingChangeWeight + BlastRadiusWeight + EnvironmentWeight) / 3
/// </summary>
public sealed class ChangeScoreCalculator : IChangeScoreCalculator
{
    private const decimal NumberOfFactors = 3m;

    /// <inheritdoc />
    public ScoreFactors Compute(
        ChangeLevel changeLevel,
        string environment,
        BlastRadiusReport? blastRadius)
    {
        var (bcWeight, bcReason) = ComputeBreakingChangeWeight(changeLevel);
        var (brWeight, brReason) = ComputeBlastRadiusWeight(blastRadius);
        var (envWeight, envReason) = ComputeEnvironmentWeight(environment);

        var score = Math.Round((bcWeight + brWeight + envWeight) / NumberOfFactors, 4);

        var source = blastRadius is not null
            ? "auto:change_level+blast_radius+environment"
            : "auto:change_level+environment (blast_radius_pending)";

        return new ScoreFactors(
            BreakingChangeWeight: bcWeight,
            BlastRadiusWeight: brWeight,
            EnvironmentWeight: envWeight,
            ComputedScore: score,
            BreakingChangeReason: bcReason,
            BlastRadiusReason: brReason,
            EnvironmentReason: envReason,
            ScoreSource: source);
    }

    private static (decimal Weight, string Reason) ComputeBreakingChangeWeight(ChangeLevel level)
        => level switch
        {
            ChangeLevel.Operational => (0.0m, "Operational change — no contract change"),
            ChangeLevel.NonBreaking => (0.1m, "NonBreaking (patch) — minimal contract risk"),
            ChangeLevel.Additive    => (0.4m, "Additive (minor) — additive contract change"),
            ChangeLevel.Breaking    => (1.0m, "Breaking (major) — breaking contract change, highest risk"),
            ChangeLevel.Publication => (0.1m, "Publication only — no new version"),
            _                       => (0.5m, $"Unknown ChangeLevel '{level}' — medium default")
        };

    private static (decimal Weight, string Reason) ComputeBlastRadiusWeight(BlastRadiusReport? report)
    {
        if (report is null)
            return (0.0m, "BlastRadius not yet calculated — weight deferred to 0.0");

        return report.TotalAffectedConsumers switch
        {
            0               => (0.0m, "No affected consumers"),
            <= 5            => (0.3m, $"{report.TotalAffectedConsumers} affected consumer(s) — low blast radius"),
            <= 20           => (0.6m, $"{report.TotalAffectedConsumers} affected consumers — medium blast radius"),
            _               => (1.0m, $"{report.TotalAffectedConsumers} affected consumers — high blast radius")
        };
    }

    private static (decimal Weight, string Reason) ComputeEnvironmentWeight(string environment)
    {
        var env = (environment ?? string.Empty).ToLowerInvariant().Trim();

        if (env is "production" or "prod" or "prd")
            return (1.0m, $"Production environment '{environment}' — highest deployment risk");

        if (env is "staging" or "pre-prod" or "preprod" or "uat" or "qa")
            return (0.6m, $"Pre-production environment '{environment}' — moderate deployment risk");

        if (env is "development" or "dev" or "local")
            return (0.2m, $"Development environment '{environment}' — low deployment risk");

        return (0.4m, $"Unknown environment '{environment}' — medium default risk");
    }
}
