using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultModels;

/// <summary>
/// Feature: SeedDefaultModels — popula a tabela ai_models com os modelos padrão
/// do <see cref="DefaultModelCatalog"/> quando ainda não existem modelos com o
/// mesmo nome no Model Registry.
///
/// Objetivo de produto: permitir que o PlatformAdmin inicialize o Model Registry
/// com modelos pré-configurados (internos e externos) para operação imediata,
/// viabilizando posterior personalização sem perder a referência do catálogo.
///
/// Comportamento idempotente: modelos já existentes (por nome) não são duplicados.
/// </summary>
public static class SeedDefaultModels
{
    /// <summary>Comando sem parâmetros — a seed é determinística a partir do catálogo.</summary>
    public sealed record Command : ICommand<Response>;

    /// <summary>Resposta com contagem de modelos criados.</summary>
    public sealed record Response(int ModelsSeeded, int TotalInCatalog);

    /// <summary>Handler que popula modelos padrão do catálogo para nomes ainda não registados.</summary>
    public sealed class Handler(
        IAiModelRepository modelRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var catalog = DefaultModelCatalog.GetAll();
            var existing = await modelRepository.ListAsync(
                provider: null, modelType: null, status: null, isInternal: null, cancellationToken);

            var existingNames = new HashSet<string>(
                existing.Select(m => m.Name),
                StringComparer.OrdinalIgnoreCase);

            var seeded = 0;
            var now = dateTimeProvider.UtcNow;

            foreach (var def in catalog)
            {
                if (existingNames.Contains(def.Name))
                    continue;

                var model = AIModel.Register(
                    name: def.Name,
                    displayName: def.DisplayName,
                    provider: def.Provider,
                    modelType: def.ModelType,
                    isInternal: def.IsInternal,
                    capabilities: def.Capabilities,
                    sensitivityLevel: def.SensitivityLevel,
                    registeredAt: now,
                    category: def.Category,
                    isDefaultForChat: def.IsDefaultForChat,
                    isDefaultForReasoning: def.IsDefaultForReasoning,
                    isDefaultForEmbeddings: def.IsDefaultForEmbeddings,
                    supportsStreaming: def.SupportsStreaming,
                    supportsToolCalling: def.SupportsToolCalling,
                    supportsEmbeddings: def.SupportsEmbeddings,
                    supportsVision: def.SupportsVision,
                    supportsStructuredOutput: def.SupportsStructuredOutput,
                    contextWindow: def.ContextWindow,
                    requiresGpu: def.RequiresGpu,
                    licenseName: def.LicenseName);

                await modelRepository.AddAsync(model, cancellationToken);
                seeded++;
            }

            return new Response(seeded, catalog.Count);
        }
    }
}
