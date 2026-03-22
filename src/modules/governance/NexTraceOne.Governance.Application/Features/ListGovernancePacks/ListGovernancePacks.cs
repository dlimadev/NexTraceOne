using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ListGovernancePacks;

/// <summary>
/// Feature: ListGovernancePacks — catálogo de governance packs disponíveis na plataforma.
/// Retorna packs configurados com categoria, status, versão e métricas de abrangência.
/// </summary>
public static class ListGovernancePacks
{
    /// <summary>Query para listar governance packs. Permite filtragem por categoria e status.</summary>
    public sealed record Query(
        string? Category = null,
        string? Status = null) : IQuery<Response>;

    /// <summary>Handler que retorna o catálogo de governance packs.</summary>
    public sealed class Handler(
        IGovernancePackRepository packRepository,
        IGovernancePackVersionRepository versionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Parse de filtros opcionais
            GovernanceRuleCategory? categoryFilter = null;
            if (!string.IsNullOrEmpty(request.Category) &&
                Enum.TryParse<GovernanceRuleCategory>(request.Category, ignoreCase: true, out var cat))
                categoryFilter = cat;

            GovernancePackStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(request.Status) &&
                Enum.TryParse<GovernancePackStatus>(request.Status, ignoreCase: true, out var st))
                statusFilter = st;

            var packs = await packRepository.ListAsync(categoryFilter, statusFilter, cancellationToken);

            // Build DTOs with rule counts from latest versions
            var dtos = new List<GovernancePackDto>();
            foreach (var p in packs)
            {
                var latestVersion = await versionRepository.GetLatestByPackIdAsync(p.Id, cancellationToken);
                int ruleCount = latestVersion?.Rules.Count ?? 0;

                dtos.Add(new GovernancePackDto(
                    PackId: p.Id.Value.ToString(),
                    Name: p.Name,
                    DisplayName: p.DisplayName,
                    Description: p.Description ?? string.Empty,
                    Category: p.Category,
                    Status: p.Status,
                    CurrentVersion: p.CurrentVersion ?? "0.0.0",
                    ScopeCount: 0,   // TODO: enriquecer com contagem real de scopes (future work)
                    RuleCount: ruleCount,
                    CreatedAt: p.CreatedAt));
            }

            var response = new Response(
                TotalPacks: dtos.Count,
                PublishedCount: dtos.Count(p => p.Status == GovernancePackStatus.Published),
                DraftCount: dtos.Count(p => p.Status == GovernancePackStatus.Draft),
                Packs: dtos);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com lista de governance packs.</summary>
    public sealed record Response(
        int TotalPacks,
        int PublishedCount,
        int DraftCount,
        IReadOnlyList<GovernancePackDto> Packs);

    /// <summary>DTO de um governance pack.</summary>
    public sealed record GovernancePackDto(
        string PackId,
        string Name,
        string DisplayName,
        string Description,
        GovernanceRuleCategory Category,
        GovernancePackStatus Status,
        string CurrentVersion,
        int ScopeCount,
        int RuleCount,
        DateTimeOffset CreatedAt);
}
