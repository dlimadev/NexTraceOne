using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetSkillDetails;

/// <summary>
/// Feature: GetSkillDetails — obtém os detalhes completos de uma skill de IA.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class GetSkillDetails
{
    /// <summary>Query para obter detalhes de uma skill pelo identificador.</summary>
    public sealed record Query(Guid SkillId) : IQuery<Response>;

    /// <summary>Handler que obtém os detalhes completos de uma skill.</summary>
    public sealed class Handler(
        IAiSkillRepository skillRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var skill = await skillRepository.GetByIdAsync(
                AiSkillId.From(request.SkillId), cancellationToken);

            if (skill is null)
                return AiGovernanceErrors.SkillNotFound(request.SkillId.ToString());

            return new Response(
                SkillId: skill.Id.Value,
                Name: skill.Name,
                DisplayName: skill.DisplayName,
                Description: skill.Description,
                SkillContent: skill.SkillContent,
                Version: skill.Version,
                Status: skill.Status.ToString(),
                OwnershipType: skill.OwnershipType.ToString(),
                Visibility: skill.Visibility.ToString(),
                Tags: skill.Tags,
                RequiredTools: skill.RequiredTools,
                PreferredModels: skill.PreferredModels,
                InputSchema: skill.InputSchema,
                OutputSchema: skill.OutputSchema,
                IsComposable: skill.IsComposable,
                ExecutionCount: skill.ExecutionCount,
                AverageRating: skill.AverageRating,
                ParentAgentId: skill.ParentAgentId);
        }
    }

    /// <summary>Resposta com detalhes completos de uma skill.</summary>
    public sealed record Response(
        Guid SkillId,
        string Name,
        string DisplayName,
        string Description,
        string SkillContent,
        string Version,
        string Status,
        string OwnershipType,
        string Visibility,
        string Tags,
        string RequiredTools,
        string PreferredModels,
        string InputSchema,
        string OutputSchema,
        bool IsComposable,
        long ExecutionCount,
        double AverageRating,
        string? ParentAgentId);
}
