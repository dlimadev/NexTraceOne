using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ListNotebooks;

/// <summary>
/// Feature: ListNotebooks — lista notebooks do tenant com filtros.
/// Wave V3.4 — AI-assisted Dashboard Creation &amp; Notebook Mode.
/// </summary>
public static class ListNotebooks
{
    public sealed record Query(
        string TenantId,
        string? Persona,
        string? Status,
        int Page,
        int PageSize) : IQuery<Response>;

    public sealed record NotebookSummaryDto(
        Guid NotebookId,
        string Title,
        string? Description,
        string Persona,
        string Status,
        string SharingScope,
        int CellCount,
        int CurrentRevisionNumber,
        Guid? LinkedDashboardId,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public sealed record Response(
        IReadOnlyList<NotebookSummaryDto> Items,
        int Total,
        int Page,
        int PageSize);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public sealed class Handler(INotebookRepository repo) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            NotebookStatus? statusFilter = request.Status is not null
                && Enum.TryParse<NotebookStatus>(request.Status, ignoreCase: true, out var s)
                ? s : null;

            var items = await repo.ListAsync(
                request.TenantId,
                request.Persona,
                statusFilter,
                request.Page,
                request.PageSize,
                cancellationToken);

            var total = await repo.CountAsync(
                request.TenantId,
                request.Persona,
                statusFilter,
                cancellationToken);

            var dtos = items.Select(n => new NotebookSummaryDto(
                n.Id.Value,
                n.Title,
                n.Description,
                n.Persona,
                n.Status.ToString(),
                n.SharingPolicy.Scope.ToString(),
                n.Cells.Count,
                n.CurrentRevisionNumber,
                n.LinkedDashboardId?.Value,
                n.CreatedAt,
                n.UpdatedAt)).ToList();

            return Result<Response>.Success(new Response(dtos, total, request.Page, request.PageSize));
        }
    }
}
