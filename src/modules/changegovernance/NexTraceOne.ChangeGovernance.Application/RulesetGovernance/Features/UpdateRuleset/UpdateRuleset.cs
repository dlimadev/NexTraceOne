using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;

namespace NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.UpdateRuleset;

/// <summary>
/// Feature: UpdateRuleset — actualiza o conteúdo (regras Spectral) de um ruleset existente a
/// partir do gestor de rulesets. Estrutura VSA: Command + Validator + Handler + Response.
/// </summary>
public static class UpdateRuleset
{
    /// <summary>Corpo HTTP do PUT (o RulesetId vem da rota).</summary>
    public sealed record UpdateBody(string Content);

    /// <summary>Comando de actualização de conteúdo de um ruleset.</summary>
    public sealed record Command(Guid RulesetId, string Content) : ICommand<Response>;

    /// <summary>Valida o comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RulesetId).NotEmpty();
            RuleFor(x => x.Content).NotEmpty().MaximumLength(200_000);
        }
    }

    /// <summary>Resposta da actualização.</summary>
    public sealed record Response(Guid RulesetId);

    /// <summary>Handler que actualiza o conteúdo do ruleset.</summary>
    public sealed class Handler(
        IRulesetRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var ruleset = await repository.GetByIdAsync(RulesetId.From(request.RulesetId), cancellationToken);
            if (ruleset is null)
                return Error.NotFound("Ruleset.NotFound", $"Ruleset {request.RulesetId} not found.");

            ruleset.UpdateContent(request.Content);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(ruleset.Id.Value);
        }
    }
}
