using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação do catálogo de modelos. Consulta o Model Registry (repositório de governança)
/// para resolver modelos por finalidade ou ID. DeepSeek não é hardcoded — é resolvido por query.
/// </summary>
public sealed class AiModelCatalogService : IAiModelCatalogService
{
    private readonly IAiModelRepository _modelRepository;

    public AiModelCatalogService(IAiModelRepository modelRepository)
    {
        _modelRepository = modelRepository;
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
}
