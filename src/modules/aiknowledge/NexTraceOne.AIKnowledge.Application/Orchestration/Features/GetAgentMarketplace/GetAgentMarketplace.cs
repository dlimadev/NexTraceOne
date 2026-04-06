using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.GetAgentMarketplace;

/// <summary>
/// Feature: GetAgentMarketplace — lista todos os agents disponíveis na plataforma com
/// metadados completos, estatísticas de uso e filtros por categoria, pesquisa e estado oficial.
/// Serve como marketplace de agents para descoberta e seleção contextual por qualquer persona.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class GetAgentMarketplace
{
    // ── QUERY ─────────────────────────────────────────────────────────────

    /// <summary>Query para listar agents do marketplace com filtros e paginação.</summary>
    public sealed record Query(
        string? Category,
        string? Search,
        bool? IsOfficial,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
            RuleFor(x => x.Search).MaximumLength(300).When(x => x.Search is not null);
            RuleFor(x => x.Category).MaximumLength(100).When(x => x.Category is not null);
        }
    }

    // ── DTO ───────────────────────────────────────────────────────────────

    /// <summary>Dados de um agent no marketplace, incluindo metadados e estatísticas de uso.</summary>
    public sealed record MarketplaceAgentDto(
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
        int Version,
        long ExecutionCount,
        string PublicationStatus,
        string OwnershipType,
        IReadOnlyList<string> Tags);

    // ── HANDLER ───────────────────────────────────────────────────────────

    /// <summary>
    /// Handler que recupera agents ativos e publicados do repositório,
    /// aplicando filtros em memória por categoria, pesquisa e estado oficial.
    /// </summary>
    public sealed class Handler(
        IAiAgentRepository agentRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var agents = await agentRepository.ListAsync(
                isActive: true,
                isOfficial: request.IsOfficial,
                ct: cancellationToken);

            var filtered = agents.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.Category))
                filtered = filtered.Where(a => string.Equals(a.Category.ToString(), request.Category, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim();
                filtered = filtered.Where(a =>
                    a.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    a.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    a.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var matchingAgents = filtered
                .OrderBy(a => a.SortOrder)
                .ThenBy(a => a.DisplayName)
                .ToList();

            var totalCount = matchingAgents.Count;

            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

            var pagedAgents = matchingAgents
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var items = pagedAgents.Select(a => new MarketplaceAgentDto(
                AgentId: a.Id.Value,
                Name: a.Name,
                DisplayName: a.DisplayName,
                Slug: a.Slug,
                Description: a.Description,
                Category: a.Category.ToString(),
                IsOfficial: a.IsOfficial,
                IsActive: a.IsActive,
                Capabilities: a.Capabilities,
                TargetPersona: a.TargetPersona,
                Icon: a.Icon,
                Version: a.Version,
                ExecutionCount: a.ExecutionCount,
                PublicationStatus: a.PublicationStatus.ToString(),
                OwnershipType: a.OwnershipType.ToString(),
                Tags: string.IsNullOrWhiteSpace(a.Capabilities)
                    ? []
                    : a.Capabilities.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            )).ToList();

            var categories = matchingAgents
                .Select(a => a.Category.ToString())
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return new Response(items, totalCount, categories);
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    /// <summary>Resposta do marketplace de agents com lista paginada, total e categorias disponíveis.</summary>
    public sealed record Response(
        IReadOnlyList<MarketplaceAgentDto> Items,
        int TotalCount,
        IReadOnlyList<string> Categories);
}
