using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Errors;

namespace NexTraceOne.ChangeIntelligence.Application.Features.RegisterRollback;

/// <summary>
/// Feature: RegisterRollback — registra um rollback de uma Release para outra Release original.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RegisterRollback
{
    /// <summary>Comando de registro de rollback de uma Release.</summary>
    public sealed record Command(Guid ReleaseId, Guid OriginalReleaseId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registro de rollback.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.OriginalReleaseId).NotEmpty();
        }
    }

    /// <summary>Handler que registra um rollback de uma Release.</summary>
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

            var result = release.RegisterRollback(ReleaseId.From(request.OriginalReleaseId));
            if (result.IsFailure)
                return result.Error;

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(release.Id.Value, request.OriginalReleaseId);
        }
    }

    /// <summary>Resposta do registro de rollback da Release.</summary>
    public sealed record Response(Guid ReleaseId, Guid OriginalReleaseId);
}
