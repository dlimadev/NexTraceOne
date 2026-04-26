using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.GetNotebook;

/// <summary>
/// Feature: GetNotebook — obtém uma notebook pelo identificador.
/// Wave V3.4 — AI-assisted Dashboard Creation &amp; Notebook Mode.
/// </summary>
public static class GetNotebook
{
    public sealed record Query(Guid NotebookId, string TenantId) : IQuery<Response>;

    public sealed record CellDto(
        Guid CellId,
        string CellType,
        int SortOrder,
        string Content,
        string? OutputJson,
        bool IsCollapsed,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public sealed record Response(
        Guid NotebookId,
        string Title,
        string? Description,
        string TenantId,
        string CreatedByUserId,
        string? TeamId,
        string Persona,
        string Status,
        string SharingScope,
        int CurrentRevisionNumber,
        Guid? LinkedDashboardId,
        IReadOnlyList<CellDto> Cells,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.NotebookId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    public sealed class Handler(INotebookRepository repo) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var notebook = await repo.GetByIdAsync(
                new NotebookId(request.NotebookId),
                request.TenantId,
                cancellationToken);

            if (notebook is null)
                return Error.NotFound("Notebook.NotFound", $"Notebook {request.NotebookId} not found.");

            return Result<Response>.Success(MapToResponse(notebook));
        }

        internal static Response MapToResponse(Notebook notebook)
            => new(
                notebook.Id.Value,
                notebook.Title,
                notebook.Description,
                notebook.TenantId,
                notebook.CreatedByUserId,
                notebook.TeamId,
                notebook.Persona,
                notebook.Status.ToString(),
                notebook.SharingPolicy.Scope.ToString(),
                notebook.CurrentRevisionNumber,
                notebook.LinkedDashboardId?.Value,
                notebook.Cells.Select(c => new CellDto(
                    c.Id.Value,
                    c.CellType.ToString(),
                    c.SortOrder,
                    c.Content,
                    c.OutputJson,
                    c.IsCollapsed,
                    c.CreatedAt,
                    c.UpdatedAt)).ToList(),
                notebook.CreatedAt,
                notebook.UpdatedAt);
    }
}
