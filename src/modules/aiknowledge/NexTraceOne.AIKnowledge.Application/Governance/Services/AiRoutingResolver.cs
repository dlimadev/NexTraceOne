using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Implementação do serviço de resolução de roteamento de modelo para o assistente de IA.
/// Encapsula a lógica de seleção de modelo via routing strategies e model catalog,
/// construção de justificativa e classificação de custo.
/// </summary>
public sealed class AiRoutingResolver(
    IAiModelCatalogService modelCatalogService,
    ILogger<AiRoutingResolver> logger) : IAiRoutingResolver
{
    /// <summary>
    /// Identificador do modelo de sistema usado como fallback quando o catalog não retorna nenhum
    /// modelo configurado. Garante que nenhuma string vazia alcance o provider. (E-A03)
    /// </summary>
    private const string SystemFallbackModel = "llama3.2";

    /// <summary>Identificador do provider padrão associado ao modelo de fallback. (E-A03)</summary>
    private const string SystemFallbackProvider = "ollama";

    public async Task<RoutingResolutionResult> ResolveRoutingAsync(
        string persona,
        AIUseCaseType useCaseType,
        string clientType,
        Guid? preferredModelId,
        string confidenceLevel,
        IReadOnlyList<AIRoutingStrategy> activeStrategies,
        CancellationToken cancellationToken = default)
    {
        var applicableStrategy = activeStrategies
            .Where(s => s.IsApplicable(persona, useCaseType.ToString(), clientType))
            .OrderBy(s => s.Priority)
            .FirstOrDefault();

        var routingPath = applicableStrategy?.PreferredPath ?? AIRoutingPath.InternalOnly;
        var (selectedModel, selectedProvider, isInternal) = SelectModel(useCaseType, persona, routingPath);

        // Override with model catalog resolution
        ResolvedModel? resolvedModel = preferredModelId.HasValue
            ? await modelCatalogService.ResolveModelByIdAsync(preferredModelId.Value, cancellationToken)
            : await modelCatalogService.ResolveDefaultModelAsync("chat", cancellationToken);

        if (resolvedModel is not null)
        {
            selectedModel = resolvedModel.ModelName;
            selectedProvider = resolvedModel.ProviderId;
            isInternal = resolvedModel.IsInternal;
        }
        else if (string.IsNullOrWhiteSpace(selectedModel))
        {
            // E-A03: quando o catalog não retorna modelo algum e SelectModel também retorna vazio,
            // usar o fallback de sistema para evitar que uma string vazia chegue ao provider.
            logger.LogWarning(
                "No AI model resolved from catalog for useCaseType={UseCaseType} persona={Persona} — " +
                "applying system fallback model '{FallbackModel}'.",
                useCaseType, persona, SystemFallbackModel);

            selectedModel = SystemFallbackModel;
            selectedProvider = SystemFallbackProvider;
            isInternal = true;
        }

        var costClass = isInternal
            ? (useCaseType is AIUseCaseType.ContractGeneration or AIUseCaseType.ChangeAnalysis ? "medium" : "low")
            : "high";

        var escalationReason = isInternal
            ? AIEscalationReason.None.ToString()
            : AIEscalationReason.ComplexityRequiresAdvancedModel.ToString();

        var rationale = BuildRoutingRationale(
            persona, useCaseType, routingPath, selectedModel, isInternal,
            applicableStrategy?.Name, confidenceLevel);

        return new RoutingResolutionResult(
            selectedModel,
            selectedProvider,
            isInternal,
            routingPath,
            rationale,
            costClass,
            escalationReason,
            applicableStrategy);
    }

    private static (string Model, string Provider, bool IsInternal) SelectModel(
        AIUseCaseType useCaseType, string persona, AIRoutingPath routingPath)
    {
        if (routingPath == AIRoutingPath.ExternalEscalation)
            return (string.Empty, string.Empty, false);
        return (string.Empty, string.Empty, true);
    }

    private static string BuildRoutingRationale(
        string persona, AIUseCaseType useCaseType, AIRoutingPath routingPath,
        string selectedModel, bool isInternal, string? strategyName, string confidenceLevel)
    {
        var parts = new List<string>
        {
            $"Use case: {useCaseType} (persona: {persona})",
            $"Routing: {routingPath}, model: {selectedModel} (internal: {isInternal})",
            $"Confidence: {confidenceLevel}"
        };
        if (strategyName is not null) parts.Add($"Strategy: {strategyName}");
        return string.Join(". ", parts) + ".";
    }
}
