using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.RulesetGovernance.Application.Abstractions;
using NexTraceOne.RulesetGovernance.Domain.Entities;

namespace NexTraceOne.RulesetGovernance.Application.Features.UploadRuleset;

/// <summary>
/// Feature: UploadRuleset — faz upload de um novo ruleset customizável.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class UploadRuleset
{
    /// <summary>Comando de upload de um novo ruleset.</summary>
    public sealed record Command(
        string Name,
        string Description,
        string Content,
        RulesetType RulesetType) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de upload de ruleset.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Content).NotEmpty();
            RuleFor(x => x.RulesetType).IsInEnum();
        }
    }

    /// <summary>Handler que cria um novo Ruleset a partir do upload.</summary>
    public sealed class Handler(
        IRulesetRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        /// <summary>Processa o comando de upload de ruleset.</summary>
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var ruleset = Ruleset.Create(
                request.Name,
                request.Description,
                request.Content,
                request.RulesetType,
                dateTimeProvider.UtcNow);

            repository.Add(ruleset);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                ruleset.Id.Value,
                ruleset.Name,
                ruleset.RulesetType.ToString(),
                ruleset.IsActive,
                ruleset.RulesetCreatedAt);
        }
    }

    /// <summary>Resposta da criação do ruleset.</summary>
    public sealed record Response(
        Guid RulesetId,
        string Name,
        string RulesetType,
        bool IsActive,
        DateTimeOffset CreatedAt);
}
