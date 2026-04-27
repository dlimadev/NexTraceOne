using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.UpdateNotebook;

/// <summary>
/// Feature: UpdateNotebook — atualiza título, células e metadados de uma notebook.
/// Wave V3.4 — AI-assisted Dashboard Creation &amp; Notebook Mode.
/// </summary>
public static class UpdateNotebook
{
    public sealed record CellDto(
        string? CellId,
        string CellType,
        int SortOrder,
        string Content);

    public sealed record Command(
        Guid NotebookId,
        string TenantId,
        string UserId,
        string Title,
        string? Description,
        string? TeamId,
        IReadOnlyList<CellDto> Cells) : ICommand<Response>;

    public sealed record Response(
        Guid NotebookId,
        int CurrentRevisionNumber,
        int CellCount);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.NotebookId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        }
    }

    public sealed class Handler(
        INotebookRepository repo,
        IGovernanceUnitOfWork uow,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var notebook = await repo.GetByIdAsync(
                new NotebookId(request.NotebookId),
                request.TenantId,
                cancellationToken);

            if (notebook is null)
                return Error.NotFound("Notebook.NotFound", $"Notebook {request.NotebookId} not found.");

            var now = clock.UtcNow;

            var cells = request.Cells
                .OrderBy(c => c.SortOrder)
                .Select(dto =>
                {
                    var id = dto.CellId is not null && Guid.TryParse(dto.CellId, out var parsed)
                        ? new NotebookCellId(parsed)
                        : new NotebookCellId(Guid.NewGuid());

                    var cellType = Enum.TryParse<NotebookCellType>(dto.CellType, ignoreCase: true, out var ct)
                        ? ct
                        : NotebookCellType.Markdown;

                    // Preserve existing output if cell already existed
                    var existing = notebook.Cells.FirstOrDefault(c => c.Id == id);

                    return new NotebookCell(
                        id,
                        cellType,
                        dto.SortOrder,
                        dto.Content,
                        existing?.OutputJson,
                        false,
                        existing?.CreatedAt ?? now,
                        now);
                })
                .ToList();

            notebook.Update(request.Title, request.Description, cells, request.TeamId, now);
            await repo.UpdateAsync(notebook, cancellationToken);
            await uow.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                notebook.Id.Value,
                notebook.CurrentRevisionNumber,
                notebook.Cells.Count));
        }
    }
}
