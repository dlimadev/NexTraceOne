using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação do catálogo de modelos. Consulta o Model Registry (repositório de governança)
/// para resolver modelos por finalidade, ID ou vinculação de feature.
/// </summary>
public sealed class AiModelCatalogService : IAiModelCatalogService
{
    private readonly IAiModelRepository _modelRepository;
    private readonly IAiFeatureModelBindingRepository _featureBindingRepository;
    private readonly ILogger<AiModelCatalogService> _logger;

    public AiModelCatalogService(
        IAiModelRepository modelRepository,
        IAiFeatureModelBindingRepository featureBindingRepository,
        ILogger<AiModelCatalogService> logger)
    {
        _modelRepository = modelRepository;
        _featureBindingRepository = featureBindingRepository;
        _logger = logger;
    }

    public async Task<ResolvedModel?> ResolveDefaultModelAsync(
        string purpose,
        CancellationToken cancellationToken = default)
    {
        var modelType = purpose?.ToLowerInvariant() switch
        {
            "chat" => ModelType.Chat,
            "completion" => ModelType.Completion,
            "embedding" => ModelType.Embedding,
            "code" => ModelType.CodeGeneration,
            "analysis" => ModelType.Analysis,
            _ => ModelType.Chat
        };

        var models = await _modelRepository.ListAsync(
            provider: null,
            modelType: modelType,
            status: ModelStatus.Active,
            isInternal: null,
            ct: cancellationToken);

        if (models.Count == 0)
            return null;

        var model = models[0];
        return new ResolvedModel(
            model.Id.Value,
            model.Name,
            model.DisplayName,
            model.Provider,
            model.Provider,
            model.IsInternal,
            model.Capabilities,
            model.ContextWindow);
    }

    public async Task<ResolvedModel?> ResolveModelByIdAsync(
        Guid modelId,
        CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetByIdAsync(
            Domain.Governance.Entities.AIModelId.From(modelId),
            cancellationToken);

        if (model is null)
            return null;

        return new ResolvedModel(
            model.Id.Value,
            model.Name,
            model.DisplayName,
            model.Provider,
            model.Provider,
            model.IsInternal,
            model.Capabilities,
            model.ContextWindow);
    }

    public async Task<ResolvedModel?> ResolveModelForFeatureAsync(
        string featureKey,
        string fallbackPurpose,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(featureKey) && tenantId != Guid.Empty)
        {
            var binding = await _featureBindingRepository.GetByFeatureKeyAsync(
                featureKey, tenantId, cancellationToken);

            if (binding is not null && binding.IsActive)
            {
                var requiredModel = await ResolveModelByIdAsync(binding.RequiredModelId, cancellationToken);
                if (requiredModel is not null)
                {
                    _logger.LogDebug(
                        "Feature binding for '{FeatureKey}' resolved to model '{Model}' ({Provider})",
                        featureKey, requiredModel.ModelName, requiredModel.ProviderId);
                    return requiredModel;
                }

                // Required model unavailable — try fallback
                if (binding.FallbackModelId.HasValue)
                {
                    var fallbackModel = await ResolveModelByIdAsync(
                        binding.FallbackModelId.Value, cancellationToken);

                    if (fallbackModel is not null)
                    {
                        _logger.LogWarning(
                            "Feature binding for '{FeatureKey}': required model {RequiredId} unavailable, using fallback '{Fallback}'",
                            featureKey, binding.RequiredModelId, fallbackModel.ModelName);
                        return fallbackModel;
                    }
                }

                _logger.LogWarning(
                    "Feature binding for '{FeatureKey}' exists but both required and fallback models are unavailable — using default for purpose '{Purpose}'",
                    featureKey, fallbackPurpose);
            }
        }

        return await ResolveDefaultModelAsync(fallbackPurpose, cancellationToken);
    }
}
