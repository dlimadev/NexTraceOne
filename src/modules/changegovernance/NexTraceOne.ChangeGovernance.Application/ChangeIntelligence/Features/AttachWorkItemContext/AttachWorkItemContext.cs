using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Errors;

namespace NexTraceOne.ChangeIntelligence.Application.Features.AttachWorkItemContext;

/// <summary>
/// Feature: AttachWorkItemContext — associa uma referência de work item a uma Release.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class AttachWorkItemContext
{
    /// <summary>Comando de associação de referência de work item a uma Release.</summary>
    public sealed record Command(Guid ReleaseId, string WorkItemReference) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de associação de work item.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.WorkItemReference).NotEmpty().MaximumLength(500);
        }
    }

    /// <summary>Handler que associa uma referência de work item a uma Release.</summary>
    public sealed class Handler(
        IReleaseRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var release = await repository.GetByIdAsync(ReleaseId.From(request.ReleaseId), cancellationToken);
            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            release.AttachWorkItem(request.WorkItemReference);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(release.Id.Value, release.WorkItemReference!);
        }
    }

    /// <summary>Resposta da associação de work item à Release.</summary>
    public sealed record Response(Guid ReleaseId, string WorkItemReference);
}
