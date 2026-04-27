using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.CreateNotebook;

/// <summary>
/// Feature: CreateNotebook — cria uma nova notebook operacional.
/// Wave V3.4 — AI-assisted Dashboard Creation &amp; Notebook Mode.
/// </summary>
public static class CreateNotebook
{
    public sealed record Command(
        string Title,
        string? Description,
        string TenantId,
        string UserId,
        string Persona,
        string? TeamId,
        IReadOnlyList<CellDto>? InitialCells) : ICommand<Response>;

    public sealed record CellDto(
        string CellType,
        int SortOrder,
        string Content);

    public sealed record Response(
        Guid NotebookId,
        string Title,
        int CellCount,
        string Status);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Persona).NotEmpty();
        }
    }

    public sealed class Handler(
        INotebookRepository repo,
        IGovernanceUnitOfWork uow,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;
            var notebook = Notebook.Create(
                request.Title,
                request.Description,
                request.TenantId,
                request.UserId,
                request.Persona,
                now,
                request.TeamId);

            if (request.InitialCells is { Count: > 0 })
            {
                foreach (var dto in request.InitialCells.OrderBy(c => c.SortOrder))
                {
                    var cellType = Enum.TryParse<NotebookCellType>(dto.CellType, ignoreCase: true, out var ct)
                        ? ct
                        : NotebookCellType.Markdown;

                    var cell = cellType switch
                    {
                        NotebookCellType.Query => Notebook.CreateQueryCell(dto.SortOrder, dto.Content, now),
                        NotebookCellType.Widget => Notebook.CreateWidgetCell(dto.SortOrder, dto.Content, now),
                        NotebookCellType.Ai => Notebook.CreateAiCell(dto.SortOrder, dto.Content, now),
                        _ => Notebook.CreateMarkdownCell(dto.SortOrder, dto.Content, now),
                    };
                    notebook.AddCell(cell, now);
                }
            }

            await repo.AddAsync(notebook, cancellationToken);
            await uow.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                notebook.Id.Value,
                notebook.Title,
                notebook.Cells.Count,
                notebook.Status.ToString()));
        }
    }
}
