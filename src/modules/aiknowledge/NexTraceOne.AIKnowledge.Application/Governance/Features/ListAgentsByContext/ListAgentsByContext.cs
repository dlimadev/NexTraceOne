using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListAgentsByContext;

/// <summary>
/// Feature: ListAgentsByContext — lista agents recomendados para um contexto de módulo específico.
///
/// Permite que os módulos do produto apresentem apenas os agents relevantes para o contexto actual:
/// - rest-api → agents de ApiDesign, TestGeneration, ContractGovernance
/// - soap     → agents de SoapDesign, TestGeneration
/// - kafka    → agents de EventDesign, TestGeneration, ContractGovernance
/// - testing  → agents de TestGeneration, ApiDesign, EventDesign
///
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListAgentsByContext
{
    /// <summary>Query para listar agents recomendados para um contexto.</summary>
    /// <param name="ModuleContext">Contexto do módulo: rest-api, soap, kafka, testing.</param>
    public sealed record Query(string ModuleContext) : IQuery<Response>;

    /// <summary>Validador da query ListAgentsByContext.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ModuleContext).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>Handler que resolve agents por contexto de módulo.</summary>
    public sealed class Handler(IAiAgentRepository agentRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var categories = ResolveCategories(request.ModuleContext);

            if (categories.Count == 0)
                return new Response([], request.ModuleContext, 0);

            var agents = await agentRepository.ListByCategoriesAsync(
                categories, isActive: true, cancellationToken);

            var items = agents
                .Select(a => new AgentItem(
                    a.Id.Value,
                    a.Name,
                    a.DisplayName,
                    a.Slug,
                    a.Description,
                    a.Category.ToString(),
                    a.IsOfficial,
                    a.IsActive,
                    a.Capabilities,
                    a.TargetPersona,
                    a.Icon,
                    a.PreferredModelId,
                    a.OwnershipType.ToString(),
                    a.PublicationStatus.ToString(),
                    a.Version,
                    a.ExecutionCount))
                .ToList();

            return new Response(items, request.ModuleContext, items.Count);
        }

        /// <summary>
        /// Mapeia o contexto de módulo para as categorias de agents relevantes.
        /// </summary>
        private static IReadOnlyList<AgentCategory> ResolveCategories(string context) =>
            context.ToLowerInvariant() switch
            {
                "rest-api" or "rest" or "openapi" =>
                [
                    AgentCategory.ApiDesign,
                    AgentCategory.TestGeneration,
                    AgentCategory.ContractGovernance,
                ],
                "soap" or "wsdl" =>
                [
                    AgentCategory.SoapDesign,
                    AgentCategory.TestGeneration,
                ],
                "kafka" or "event" or "asyncapi" or "event-contract" =>
                [
                    AgentCategory.EventDesign,
                    AgentCategory.TestGeneration,
                    AgentCategory.ContractGovernance,
                ],
                "testing" or "test" =>
                [
                    AgentCategory.TestGeneration,
                    AgentCategory.ApiDesign,
                    AgentCategory.EventDesign,
                ],
                _ => [],
            };
    }

    /// <summary>Resposta com agents recomendados para o contexto.</summary>
    public sealed record Response(
        IReadOnlyList<AgentItem> Items,
        string ModuleContext,
        int TotalCount);

    /// <summary>Item de agent recomendado para o contexto.</summary>
    public sealed record AgentItem(
        Guid AgentId,
        string Name,
        string DisplayName,
        string Slug,
        string Description,
        string Category,
        bool IsOfficial,
        bool IsActive,
        string Capabilities,
        string TargetPersona,
        string Icon,
        Guid? PreferredModelId,
        string OwnershipType,
        string PublicationStatus,
        int Version,
        long ExecutionCount);
}
