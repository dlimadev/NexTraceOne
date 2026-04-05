using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.TransitionEnforcementMode;

/// <summary>
/// Feature: TransitionEnforcementMode — faz a transição gradual do modo de enforcement
/// de uma política de código: Advisory → SoftEnforce → HardEnforce.
/// Suporta o padrão "gradual enforcement" que evita bloqueios abruptos.
/// </summary>
public static class TransitionEnforcementMode
{
    public sealed record Command(
        string PolicyName,
        PolicyEnforcementMode TargetMode) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PolicyName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TargetMode).IsInEnum();
        }
    }

    public sealed class Handler(
        IPolicyAsCodeRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var definition = await repository.GetByNameAsync(request.PolicyName, cancellationToken);
            if (definition is null)
                return Error.NotFound("POLICY_AS_CODE_NOT_FOUND", "Policy definition '{0}' not found.", request.PolicyName);

            if (definition.Status != PolicyDefinitionStatus.Active)
                return Error.Validation("POLICY_NOT_ACTIVE",
                    "Only Active policies can have enforcement mode transitioned. Current status: {0}.",
                    definition.Status);

            var previousMode = definition.EnforcementMode;

            try
            {
                definition.TransitionEnforcement(request.TargetMode);
            }
            catch (InvalidOperationException ex)
            {
                return Error.Validation("ENFORCEMENT_TRANSITION_INVALID", ex.Message);
            }

            await repository.UpdateAsync(definition, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                definition.Id.Value,
                definition.Name,
                previousMode,
                definition.EnforcementMode));
        }
    }

    public sealed record Response(
        Guid PolicyId,
        string PolicyName,
        PolicyEnforcementMode PreviousMode,
        PolicyEnforcementMode NewMode);
}
