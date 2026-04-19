using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListSkills;

/// <summary>
/// Feature: ListSkills — lista skills de IA com filtros opcionais.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListSkills
{
    /// <summary>Query para listagem de skills.</summary>
    public sealed record Query(
        string? Status,
        string? OwnershipType,
        Guid? TenantId) : IQuery<Response>;

    /// <summary>Handler que lista skills com filtros opcionais.</summary>
    public sealed class Handler(
        IAiSkillRepository skillRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var status = request.Status is not null
                ? Enum.Parse<SkillStatus>(request.Status, ignoreCase: true)
                : (SkillStatus?)null;

            var ownershipType = request.OwnershipType is not null
                ? Enum.Parse<SkillOwnershipType>(request.OwnershipType, ignoreCase: true)
                : (SkillOwnershipType?)null;

            var skills = await skillRepository.ListAsync(status, ownershipType, request.TenantId, cancellationToken);

            var items = skills.Select(s => new SkillSummary(
                SkillId: s.Id.Value,
                Name: s.Name,
                DisplayName: s.DisplayName,
                Description: s.Description,
                Version: s.Version,
                Status: s.Status.ToString(),
                Tags: s.Tags,
                ExecutionCount: s.ExecutionCount,
                AverageRating: s.AverageRating))
            .ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta da listagem de skills.</summary>
    public sealed record Response(IReadOnlyList<SkillSummary> Items, int TotalCount);

    /// <summary>Resumo de uma skill para listagem.</summary>
    public sealed record SkillSummary(
        Guid SkillId,
        string Name,
        string DisplayName,
        string Description,
        string Version,
        string Status,
        string Tags,
        long ExecutionCount,
        double AverageRating);
}
