using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListAvailableModels;

/// <summary>
/// Feature: ListAvailableModels — lista modelos disponíveis para o utilizador atual.
/// Aplica avaliação de políticas de acesso para retornar apenas modelos autorizados,
/// agrupados por classificação interna/externa.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListAvailableModels
{
    /// <summary>Query para listar modelos disponíveis ao utilizador atual.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que avalia políticas e retorna modelos autorizados.</summary>
    public sealed class Handler(
        IAiModelAuthorizationService authorizationService) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var authResult = await authorizationService.GetAvailableModelsAsync(cancellationToken);

            var internalModels = authResult.Models
                .Where(m => m.IsInternal)
                .Select(MapToItem)
                .ToList();

            var externalModels = authResult.Models
                .Where(m => m.IsExternal)
                .Select(MapToItem)
                .ToList();

            return new Response(
                internalModels,
                externalModels,
                authResult.AllowExternalModels,
                authResult.AppliedPolicyName,
                internalModels.Count + externalModels.Count);
        }

        private static AvailableModelItem MapToItem(AuthorizedModel m) => new(
            m.ModelId,
            m.Name,
            m.DisplayName,
            m.Provider,
            m.ModelType,
            m.IsInternal,
            m.IsExternal,
            m.Status,
            m.Capabilities,
            m.IsDefault,
            m.Slug,
            m.ContextWindow);
    }

    /// <summary>Resposta com modelos agrupados por classificação interna/externa.</summary>
    public sealed record Response(
        IReadOnlyList<AvailableModelItem> InternalModels,
        IReadOnlyList<AvailableModelItem> ExternalModels,
        bool AllowExternalModels,
        string? AppliedPolicyName,
        int TotalCount);

    /// <summary>Item de modelo disponível com metadados completos.</summary>
    public sealed record AvailableModelItem(
        Guid ModelId,
        string Name,
        string DisplayName,
        string Provider,
        string ModelType,
        bool IsInternal,
        bool IsExternal,
        string Status,
        string Capabilities,
        bool IsDefault,
        string? Slug,
        int? ContextWindow);
}
