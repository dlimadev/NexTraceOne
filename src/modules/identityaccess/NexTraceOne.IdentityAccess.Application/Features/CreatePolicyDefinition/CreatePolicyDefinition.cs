using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Application.Features.CreatePolicyDefinition;

/// <summary>
/// Feature: CreatePolicyDefinition — cria uma nova definição de política no Policy Studio.
/// Permite ao administrador de plataforma definir políticas sem código usando DSL JSON.
/// Wave D.3 — No-code Policy Studio.
/// </summary>
public static class CreatePolicyDefinition
{
    public sealed record Command(
        string TenantId,
        string Name,
        string? Description,
        PolicyDefinitionType PolicyType,
        string RulesJson,
        string ActionJson,
        string AppliesTo,
        string? EnvironmentFilter,
        string? CreatedByUserId) : ICommand<Response>;

    public sealed record Response(
        Guid PolicyDefinitionId,
        string Name,
        int Version);

    public sealed class Handler(
        IPolicyDefinitionRepository policyRepository,
        IIdentityAccessUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.NullOrWhiteSpace(request.TenantId);
            Guard.Against.NullOrWhiteSpace(request.Name);

            var now = clock.UtcNow;
            var policy = PolicyDefinition.Create(
                tenantId: request.TenantId,
                name: request.Name,
                description: request.Description,
                policyType: request.PolicyType,
                rulesJson: request.RulesJson,
                actionJson: request.ActionJson,
                appliesTo: request.AppliesTo,
                environmentFilter: request.EnvironmentFilter,
                createdByUserId: request.CreatedByUserId,
                now: now);

            policyRepository.Add(policy);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                policy.Id.Value,
                policy.Name,
                policy.Version));
        }
    }
}
