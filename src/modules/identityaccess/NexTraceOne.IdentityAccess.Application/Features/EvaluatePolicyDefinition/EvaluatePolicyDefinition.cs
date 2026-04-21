using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Application.Features.EvaluatePolicyDefinition;

/// <summary>
/// Feature: EvaluatePolicyDefinition — avalia uma política contra um contexto JSON.
/// Retorna o resultado (Allow/Warn/Block) sem persistência — é uma operação de query.
/// Wave D.3 — No-code Policy Studio.
/// </summary>
public static class EvaluatePolicyDefinition
{
    public sealed record Query(
        Guid PolicyDefinitionId,
        string ContextJson) : IQuery<Response>;

    public sealed record Response(
        Guid PolicyDefinitionId,
        string PolicyName,
        bool Passed,
        string Action,
        string? Message,
        string? RuleTriggered);

    public sealed class Handler(
        IPolicyDefinitionRepository policyRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.Default(request.PolicyDefinitionId);

            var id = PolicyDefinitionId.From(request.PolicyDefinitionId);
            var policy = await policyRepository.GetByIdAsync(id, cancellationToken);
            if (policy is null)
                return IdentityErrors.PolicyDefinitionNotFound(request.PolicyDefinitionId.ToString());

            if (!policy.IsEnabled)
                return IdentityErrors.PolicyDefinitionDisabled(policy.Name);

            var evalResult = policy.Evaluate(request.ContextJson);

            return Result<Response>.Success(new Response(
                policy.Id.Value,
                policy.Name,
                evalResult.Passed,
                evalResult.Action,
                evalResult.Message,
                evalResult.RuleTriggered));
        }
    }
}
