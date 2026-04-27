using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.ListDashboardTemplates;

/// <summary>
/// Feature: ListDashboardTemplates — lista templates da galeria interna, incluindo templates de sistema.
/// V3.8 — Marketplace, Plugin SDK &amp; Widgets de Terceiros.
/// </summary>
public static class ListDashboardTemplates
{
    public sealed record Query(
        string TenantId,
        string? Category = null,
        string? Persona = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    public sealed record TemplateDto(
        Guid Id,
        string Name,
        string Description,
        string Persona,
        string Category,
        IReadOnlyList<string> Tags,
        string Version,
        bool IsSystem,
        int InstallCount,
        DateTimeOffset CreatedAt);

    public sealed record Response(
        IReadOnlyList<TemplateDto> Items,
        int TotalCount,
        int Page,
        int PageSize);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public sealed class Handler(IDashboardTemplateRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var (items, total) = await repository.ListAsync(
                request.TenantId,
                request.Category,
                request.Persona,
                request.Page,
                request.PageSize,
                cancellationToken);

            var dtos = items.Select(t =>
            {
                var tags = System.Text.Json.JsonSerializer.Deserialize<List<string>>(t.TagsJson) ?? [];
                return new TemplateDto(
                    t.Id.Value,
                    t.Name,
                    t.Description,
                    t.Persona,
                    t.Category,
                    tags,
                    t.Version,
                    t.IsSystem,
                    t.InstallCount,
                    t.CreatedAt);
            }).ToList();

            return Result<Response>.Success(new Response(dtos, total, request.Page, request.PageSize));
        }
    }
}
