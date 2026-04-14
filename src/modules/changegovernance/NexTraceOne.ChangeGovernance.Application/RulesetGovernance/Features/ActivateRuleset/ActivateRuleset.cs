using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Errors;

namespace NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ActivateRuleset;

/// <summary>
/// Feature: ActivateRuleset — reativa um ruleset previamente arquivado.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ActivateRuleset
{
    /// <summary>Comando de ativação de um ruleset.</summary>
    public sealed record Command(Guid RulesetId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de ativação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RulesetId).NotEmpty();
        }
    }

    /// <summary>Handler que ativa um ruleset previamente arquivado.</summary>
    public sealed class Handler(
        IRulesetRepository repository,
        IRulesetGovernanceUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        /// <summary>Processa o comando de ativação de ruleset.</summary>
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var ruleset = await repository.GetByIdAsync(RulesetId.From(request.RulesetId), cancellationToken);
            if (ruleset is null)
                return RulesetGovernanceErrors.RulesetNotFound(request.RulesetId.ToString());

            ruleset.Activate();

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(ruleset.Id.Value, ruleset.IsActive);
        }
    }

    /// <summary>Resposta da ativação do ruleset.</summary>
    public sealed record Response(Guid RulesetId, bool IsActive);
}
