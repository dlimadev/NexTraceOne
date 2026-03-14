using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.RulesetGovernance.Application.Abstractions;
using NexTraceOne.RulesetGovernance.Domain.Entities;
using NexTraceOne.RulesetGovernance.Domain.Errors;

namespace NexTraceOne.RulesetGovernance.Application.Features.BindRulesetToAssetType;

/// <summary>
/// Feature: BindRulesetToAssetType — vincula um ruleset a um tipo de ativo.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class BindRulesetToAssetType
{
    /// <summary>Comando de vinculação de ruleset a tipo de ativo.</summary>
    public sealed record Command(Guid RulesetId, string AssetType) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de vinculação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RulesetId).NotEmpty();
            RuleFor(x => x.AssetType).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>Handler que vincula um ruleset a um tipo de ativo.</summary>
    public sealed class Handler(
        IRulesetRepository rulesetRepository,
        IRulesetBindingRepository bindingRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        /// <summary>Processa o comando de vinculação.</summary>
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var rulesetId = RulesetId.From(request.RulesetId);

            var ruleset = await rulesetRepository.GetByIdAsync(rulesetId, cancellationToken);
            if (ruleset is null)
                return RulesetGovernanceErrors.RulesetNotFound(request.RulesetId.ToString());

            var existing = await bindingRepository.GetByRulesetAndAssetTypeAsync(rulesetId, request.AssetType, cancellationToken);
            if (existing is not null)
                return RulesetGovernanceErrors.BindingAlreadyExists(request.RulesetId.ToString(), request.AssetType);

            var binding = RulesetBinding.Create(rulesetId, request.AssetType, dateTimeProvider.UtcNow);

            bindingRepository.Add(binding);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(binding.Id.Value, binding.RulesetId.Value, binding.AssetType);
        }
    }

    /// <summary>Resposta da vinculação do ruleset ao tipo de ativo.</summary>
    public sealed record Response(Guid BindingId, Guid RulesetId, string AssetType);
}
