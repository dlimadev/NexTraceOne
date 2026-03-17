using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Errors;

namespace NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ArchiveRuleset;

/// <summary>
/// Feature: ArchiveRuleset — arquiva (soft-disable) um ruleset existente.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ArchiveRuleset
{
    /// <summary>Comando de arquivamento de um ruleset.</summary>
    public sealed record Command(Guid RulesetId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de arquivamento.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RulesetId).NotEmpty();
        }
    }

    /// <summary>Handler que arquiva um ruleset existente.</summary>
    public sealed class Handler(
        IRulesetRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        /// <summary>Processa o comando de arquivamento de ruleset.</summary>
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var ruleset = await repository.GetByIdAsync(RulesetId.From(request.RulesetId), cancellationToken);
            if (ruleset is null)
                return RulesetGovernanceErrors.RulesetNotFound(request.RulesetId.ToString());

            var archiveResult = ruleset.Archive();
            if (archiveResult.IsFailure)
                return archiveResult.Error;

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(ruleset.Id.Value, ruleset.IsActive);
        }
    }

    /// <summary>Resposta do arquivamento do ruleset.</summary>
    public sealed record Response(Guid RulesetId, bool IsActive);
}
