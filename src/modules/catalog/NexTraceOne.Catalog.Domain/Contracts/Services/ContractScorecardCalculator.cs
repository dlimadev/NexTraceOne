using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Serviço de domínio responsável pelo cálculo do scorecard técnico de um contrato.
/// Opera sobre o modelo canônico normalizado, avaliando qualidade, completude,
/// compatibilidade e risco técnico. Cada dimensão é pontuada de 0.0 a 1.0
/// com justificativa gerada automaticamente para auditoria e workflow.
/// </summary>
public static class ContractScorecardCalculator
{
    /// <summary>
    /// Computa o scorecard técnico completo de uma versão de contrato a partir
    /// do seu modelo canônico normalizado e das violações de regras detectadas.
    /// </summary>
    /// <param name="contractVersionId">Identificador da versão de contrato.</param>
    /// <param name="model">Modelo canônico normalizado.</param>
    /// <param name="protocol">Protocolo do contrato.</param>
    /// <param name="ruleViolationCount">Número de regras violadas.</param>
    /// <param name="computedAt">Data/hora de cálculo.</param>
    /// <returns>Scorecard com todas as dimensões avaliadas.</returns>
    public static ContractScorecard Compute(
        ContractVersionId contractVersionId,
        ContractCanonicalModel model,
        ContractProtocol protocol,
        int ruleViolationCount,
        DateTimeOffset computedAt)
    {
        var quality = ComputeQualityScore(model, protocol);
        var completeness = ComputeCompletenessScore(model, protocol);
        var compatibility = ComputeCompatibilityScore(model, protocol, ruleViolationCount);
        var risk = ComputeRiskScore(model, protocol, ruleViolationCount);

        return ContractScorecard.Create(
            contractVersionId,
            protocol,
            quality.Score,
            completeness.Score,
            compatibility.Score,
            risk.Score,
            model.OperationCount,
            model.SchemaCount,
            model.HasSecurityDefinitions,
            model.HasExamples,
            model.HasDescriptions,
            quality.Justification,
            completeness.Justification,
            compatibility.Justification,
            risk.Justification,
            computedAt);
    }

    /// <summary>
    /// Avalia a qualidade geral do contrato: naming, documentação, consistência.
    /// Para WorkerService, segurança não é critério — substitui por trigger/schedule.
    /// </summary>
    private static (decimal Score, string Justification) ComputeQualityScore(ContractCanonicalModel model, ContractProtocol protocol)
    {
        var score = 0.5m;
        var factors = new List<string>();

        if (model.HasDescriptions) { score += 0.15m; factors.Add("descriptions present"); }
        else { factors.Add("missing operation descriptions (-0.15)"); }

        if (model.HasExamples) { score += 0.15m; factors.Add("examples present"); }
        else { factors.Add("missing examples (-0.15)"); }

        if (protocol == ContractProtocol.WorkerService)
        {
            // Para background services, schedule/trigger documentado substitui security como critério
            if (!string.IsNullOrWhiteSpace(model.SpecVersion)) { score += 0.10m; factors.Add("trigger type declared"); }
            else { factors.Add("missing trigger type (-0.10)"); }
        }
        else
        {
            if (model.HasSecurityDefinitions) { score += 0.10m; factors.Add("security defined"); }
            else { factors.Add("missing security definitions (-0.10)"); }
        }

        if (model.Tags.Count > 0) { score += 0.10m; factors.Add("tags for categorization"); }
        else { factors.Add("no tags defined (-0.10)"); }

        return (Math.Clamp(score, 0m, 1m), string.Join("; ", factors));
    }

    /// <summary>
    /// Avalia a completude do contrato: cobertura de schemas, tipos, metadados.
    /// Para WorkerService, servers não são critério — substitui por schedule/timeout.
    /// </summary>
    private static (decimal Score, string Justification) ComputeCompletenessScore(ContractCanonicalModel model, ContractProtocol protocol)
    {
        var score = 0.4m;
        var factors = new List<string>();

        if (model.OperationCount > 0) { score += 0.20m; factors.Add($"{model.OperationCount} operations defined"); }
        else { factors.Add("no operations defined (-0.20)"); }

        if (model.SchemaCount > 0) { score += 0.15m; factors.Add($"{model.SchemaCount} schemas defined"); }
        else { factors.Add("no schemas defined (-0.15)"); }

        if (protocol == ContractProtocol.WorkerService)
        {
            // WorkerService: schedule expression (mapped to Description) replaces servers
            if (!string.IsNullOrWhiteSpace(model.Description)) { score += 0.10m; factors.Add("schedule expression declared"); }
            else { factors.Add("no schedule expression (-0.10)"); }
        }
        else
        {
            if (model.Servers.Count > 0) { score += 0.10m; factors.Add("servers/endpoints defined"); }
            else { factors.Add("no servers defined (-0.10)"); }
        }

        if (!string.IsNullOrWhiteSpace(model.Description) || protocol == ContractProtocol.WorkerService)
        {
            if (!string.IsNullOrWhiteSpace(model.Title) && model.Title != "Unknown Worker")
            { score += 0.15m; factors.Add("service name present"); }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(model.Description)) { score += 0.15m; factors.Add("API description present"); }
            else { factors.Add("missing API description (-0.15)"); }
        }

        return (Math.Clamp(score, 0m, 1m), string.Join("; ", factors));
    }

    /// <summary>
    /// Avalia a compatibilidade baseada nas violações de regras detectadas.
    /// Para WorkerService, security score penalty não é aplicada.
    /// </summary>
    private static (decimal Score, string Justification) ComputeCompatibilityScore(
        ContractCanonicalModel model, ContractProtocol protocol, int ruleViolationCount)
    {
        var score = 1.0m;
        var factors = new List<string>();

        if (ruleViolationCount > 0)
        {
            score -= Math.Min(ruleViolationCount * 0.1m, 0.5m);
            factors.Add($"{ruleViolationCount} rule violations detected");
        }
        else
        {
            factors.Add("no rule violations");
        }

        if (protocol != ContractProtocol.WorkerService)
        {
            if (model.HasSecurityDefinitions) { factors.Add("security compliant"); }
            else { score -= 0.15m; factors.Add("missing security reduces compatibility score"); }
        }

        return (Math.Clamp(score, 0m, 1m), string.Join("; ", factors));
    }

    /// <summary>
    /// Avalia o risco técnico do contrato. Score alto = alto risco.
    /// Para WorkerService, ausência de security não é risco (não aplicável).
    /// </summary>
    private static (decimal Score, string Justification) ComputeRiskScore(
        ContractCanonicalModel model, ContractProtocol protocol, int ruleViolationCount)
    {
        var score = 0.1m;
        var factors = new List<string>();

        if (protocol != ContractProtocol.WorkerService)
        {
            if (!model.HasSecurityDefinitions) { score += 0.25m; factors.Add("no security definitions increases risk"); }
        }
        if (ruleViolationCount > 5) { score += 0.20m; factors.Add($"high violation count ({ruleViolationCount})"); }
        else if (ruleViolationCount > 0) { score += 0.10m; factors.Add($"{ruleViolationCount} rule violations"); }
        if (model.OperationCount == 0) { score += 0.15m; factors.Add("no operations increases risk"); }
        if (!model.HasDescriptions) { score += 0.10m; factors.Add("undocumented operations increase risk"); }

        if (factors.Count == 0) factors.Add("low risk profile");

        return (Math.Clamp(score, 0m, 1m), string.Join("; ", factors));
    }
}
