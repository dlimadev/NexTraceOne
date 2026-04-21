using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.UpdatePolicyDefinition;

/// <summary>
/// Feature: UpdatePolicyDefinition — actualiza as regras e acção de uma política existente.
/// Incrementa a versão automaticamente para rastreabilidade.
/// Wave D.3 — No-code Policy Studio.
/// </summary>
public static class UpdatePolicyDefinition
{
    public sealed record Command(
        Guid PolicyDefinitionId,
        string TenantId,
        string RulesJson,
        string ActionJson,
        bool IsEnabled) : ICommand<Response>;

    public sealed record Response(
        Guid PolicyDefinitionId,
        string Name,
        int Version,
        bool IsEnabled);

    public sealed class Handler(
        IPolicyDefinitionRepository policyRepository,
        IIdentityAccessUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.Default(request.PolicyDefinitionId);

            var id = PolicyDefinitionId.From(request.PolicyDefinitionId);
            var policy = await policyRepository.GetByIdAsync(id, cancellationToken);
            if (policy is null)
                return IdentityErrors.PolicyDefinitionNotFound(request.PolicyDefinitionId.ToString());

            var now = clock.UtcNow;
            policy.UpdateRules(request.RulesJson, request.ActionJson, now);

            if (request.IsEnabled)
                policy.Enable();
            else
                policy.Disable();

            policyRepository.Update(policy);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                policy.Id.Value,
                policy.Name,
                policy.Version,
                policy.IsEnabled));
        }
    }
}
