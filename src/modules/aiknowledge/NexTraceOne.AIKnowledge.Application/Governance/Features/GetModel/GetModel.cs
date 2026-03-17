using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetModel;

/// <summary>
/// Feature: GetModel — obtém os detalhes completos de um modelo de IA.
/// Retorna erro NotFound se o modelo não existir.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class GetModel
{
    /// <summary>Query de consulta de um modelo de IA pelo identificador.</summary>
    public sealed record Query(Guid ModelId) : IQuery<Response>;

    /// <summary>Handler que obtém os detalhes completos de um modelo de IA.</summary>
    public sealed class Handler(
        IAiModelRepository modelRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var model = await modelRepository.GetByIdAsync(
                AIModelId.From(request.ModelId),
                cancellationToken);

            if (model is null)
            {
                return AiGovernanceErrors.ModelNotFound(request.ModelId.ToString());
            }

            return new Response(
                model.Id.Value,
                model.Name,
                model.DisplayName,
                model.Provider,
                model.ModelType.ToString(),
                model.IsInternal,
                model.IsExternal,
                model.Status.ToString(),
                model.Capabilities,
                model.DefaultUseCases,
                model.SensitivityLevel,
                model.RegisteredAt);
        }
    }

    /// <summary>Resposta com detalhes completos de um modelo de IA.</summary>
    public sealed record Response(
        Guid ModelId,
        string Name,
        string DisplayName,
        string Provider,
        string ModelType,
        bool IsInternal,
        bool IsExternal,
        string Status,
        string Capabilities,
        string DefaultUseCases,
        int SensitivityLevel,
        DateTimeOffset RegisteredAt);
}
