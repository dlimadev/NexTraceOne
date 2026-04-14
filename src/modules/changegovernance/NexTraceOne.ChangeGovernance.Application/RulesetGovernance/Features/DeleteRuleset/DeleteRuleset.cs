using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Errors;

namespace NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.DeleteRuleset;

/// <summary>
/// Feature: DeleteRuleset — remove permanentemente um ruleset do sistema.
/// Apenas rulesets do tipo Custom podem ser eliminados.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class DeleteRuleset
{
    /// <summary>Comando de eliminação de um ruleset.</summary>
    public sealed record Command(Guid RulesetId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de eliminação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RulesetId).NotEmpty();
        }
    }

    /// <summary>Handler que elimina um ruleset existente.</summary>
    public sealed class Handler(
        IRulesetRepository repository,
        IRulesetGovernanceUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        /// <summary>Processa o comando de eliminação de ruleset.</summary>
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var ruleset = await repository.GetByIdAsync(RulesetId.From(request.RulesetId), cancellationToken);
            if (ruleset is null)
                return RulesetGovernanceErrors.RulesetNotFound(request.RulesetId.ToString());

            repository.Remove(ruleset);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(request.RulesetId);
        }
    }

    /// <summary>Resposta da eliminação do ruleset.</summary>
    public sealed record Response(Guid RulesetId);
}
