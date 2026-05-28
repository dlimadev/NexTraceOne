using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateUserModelPolicy;

/// <summary>
/// Feature: UpdateUserModelPolicy — atualiza a política de acesso a modelos de um utilizador.
/// Permite alterar allowlist, denylist, limites de tokens e permissões de IA externa.
/// </summary>
public static class UpdateUserModelPolicy
{
    /// <summary>Comando de atualização da política de modelos por utilizador.</summary>
    public sealed record Command(
        Guid PolicyId,
        string AllowedModelIds,
        string BlockedModelIds,
        bool AllowExternalAI,
        bool InternalOnly,
        int MaxTokensPerRequest,
        string Description) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PolicyId).NotEqual(Guid.Empty);
            RuleFor(x => x.MaxTokensPerRequest).GreaterThan(0);
            RuleFor(x => x.Description).MaximumLength(2000);
        }
    }

    /// <summary>Handler que atualiza a política de acesso a modelos do utilizador.</summary>
    public sealed class Handler(
        IAiAccessPolicyRepository policyRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var policy = await policyRepository.GetByIdAsync(
                AIAccessPolicyId.From(request.PolicyId), cancellationToken);

            if (policy is null)
                return Error.NotFound(
                    "UserModelPolicy.NotFound",
                    "Política '{0}' não encontrada.",
                    request.PolicyId);

            if (!string.Equals(policy.Scope, "user", StringComparison.OrdinalIgnoreCase))
                return Error.Validation(
                    "UserModelPolicy.WrongScope",
                    "Esta operação é válida apenas para políticas com scope 'user'.");

            policy.Update(
                request.Description ?? policy.Description,
                request.AllowExternalAI,
                request.InternalOnly,
                request.MaxTokensPerRequest,
                policy.EnvironmentRestrictions);

            policy.SetAllowedModels(request.AllowedModelIds ?? string.Empty);
            policy.SetBlockedModels(request.BlockedModelIds ?? string.Empty);

            await policyRepository.UpdateAsync(policy, cancellationToken);

            return new Response(policy.Id.Value);
        }
    }

    /// <summary>Resposta da atualização da política.</summary>
    public sealed record Response(Guid PolicyId);
}
