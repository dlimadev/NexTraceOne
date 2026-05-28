using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.DeleteUserModelPolicy;

/// <summary>
/// Feature: DeleteUserModelPolicy — desativa a política de acesso a modelos de um utilizador.
/// </summary>
public static class DeleteUserModelPolicy
{
    /// <summary>Comando de desativação de política por utilizador.</summary>
    public sealed record Command(Guid PolicyId) : ICommand<Response>;

    /// <summary>Handler que desativa a política de acesso a modelos.</summary>
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

            policy.Deactivate();
            await policyRepository.UpdateAsync(policy, cancellationToken);

            return new Response(request.PolicyId);
        }
    }

    /// <summary>Resposta da desativação da política.</summary>
    public sealed record Response(Guid PolicyId);
}
