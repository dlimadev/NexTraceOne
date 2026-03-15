using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.AiGovernance.Application.Abstractions;
using NexTraceOne.AiGovernance.Domain.Entities;
using NexTraceOne.AiGovernance.Domain.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AiGovernance.Application.Features.UpdatePolicy;

/// <summary>
/// Feature: UpdatePolicy — atualiza parâmetros de uma política de acesso de IA.
/// Estrutura VSA: Command + Validator + Handler num único ficheiro.
/// </summary>
public static class UpdatePolicy
{
    /// <summary>Comando de atualização de uma política de acesso de IA.</summary>
    public sealed record Command(
        Guid PolicyId,
        string Description,
        bool AllowExternalAI,
        bool InternalOnly,
        int MaxTokensPerRequest,
        string? EnvironmentRestrictions) : ICommand;

    /// <summary>Valida a entrada do comando de atualização de política.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PolicyId).NotEmpty();
            RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            RuleFor(x => x.MaxTokensPerRequest).GreaterThan(0);
        }
    }

    /// <summary>Handler que atualiza parâmetros de uma política de acesso de IA.</summary>
    public sealed class Handler(
        IAiAccessPolicyRepository policyRepository) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var policy = await policyRepository.GetByIdAsync(
                AIAccessPolicyId.From(request.PolicyId),
                cancellationToken);

            if (policy is null)
            {
                return AiGovernanceErrors.PolicyNotFound(request.PolicyId.ToString());
            }

            var result = policy.Update(
                request.Description,
                request.AllowExternalAI,
                request.InternalOnly,
                request.MaxTokensPerRequest,
                request.EnvironmentRestrictions ?? string.Empty);

            if (result.IsFailure)
            {
                return result.Error;
            }

            await policyRepository.UpdateAsync(policy, cancellationToken);

            return Unit.Value;
        }
    }
}
