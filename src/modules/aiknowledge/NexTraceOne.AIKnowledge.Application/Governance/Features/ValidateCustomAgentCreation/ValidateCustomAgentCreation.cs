using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ValidateCustomAgentCreation;

/// <summary>
/// Feature: ValidateCustomAgentCreation — valida se a criação de um agente customizado é permitida.
/// Consulta:
///   - ai.agents.custom_creation.require_approval: se true, agentes customizados precisam de aprovação
/// Pilar: AI Governance &amp; Developer Acceleration
/// </summary>
public static class ValidateCustomAgentCreation
{
    /// <summary>Query para validar criação de agente customizado.</summary>
    public sealed record Query(
        string AgentName,
        string CreatedBy,
        bool HasApproval) : IQuery<Response>;

    /// <summary>Handler que avalia permissão de criação de agente customizado.</summary>
    public sealed class Handler(
        IConfigurationResolutionService configService,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var requireApproval = await configService.ResolveEffectiveValueAsync(
                "ai.agents.custom_creation.require_approval",
                ConfigurationScope.Tenant, null, cancellationToken);

            var approvalRequired = requireApproval?.EffectiveValue == "true";

            if (!approvalRequired)
            {
                return new Response(
                    AgentName: request.AgentName,
                    IsAllowed: true,
                    ApprovalRequired: false,
                    Reason: "Custom agent creation does not require approval",
                    EvaluatedAt: dateTimeProvider.UtcNow);
            }

            return new Response(
                AgentName: request.AgentName,
                IsAllowed: request.HasApproval,
                ApprovalRequired: true,
                Reason: request.HasApproval
                    ? $"Custom agent '{request.AgentName}' creation is approved"
                    : $"Custom agent '{request.AgentName}' requires approval before creation",
                EvaluatedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da validação de criação de agente customizado.</summary>
    public sealed record Response(
        string AgentName,
        bool IsAllowed,
        bool ApprovalRequired,
        string Reason,
        DateTimeOffset EvaluatedAt);
}
