using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.DeleteNotebook;

/// <summary>
/// Feature: DeleteNotebook — remove uma notebook.
/// Wave V3.4 — AI-assisted Dashboard Creation &amp; Notebook Mode.
/// </summary>
public static class DeleteNotebook
{
    public sealed record Command(Guid NotebookId, string TenantId) : ICommand<Response>;

    public sealed record Response(Guid NotebookId, bool Deleted);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.NotebookId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    public sealed class Handler(
        INotebookRepository repo,
        IGovernanceUnitOfWork uow) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var notebook = await repo.GetByIdAsync(
                new NotebookId(request.NotebookId),
                request.TenantId,
                cancellationToken);

            if (notebook is null)
                return Error.NotFound("Notebook.NotFound", $"Notebook {request.NotebookId} not found.");

            await repo.DeleteAsync(notebook, cancellationToken);
            await uow.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(request.NotebookId, true));
        }
    }
}
