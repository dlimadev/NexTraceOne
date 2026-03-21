using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

using MediatR;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateAgent;

/// <summary>
/// Feature: UpdateAgent — atualiza definição de um agent customizado.
/// Agents System não podem ser modificados via API.
/// Estrutura VSA: Command + Validator + Handler + Response.
/// </summary>
public static class UpdateAgent
{
    /// <summary>Comando de atualização de um agent customizado.</summary>
    public sealed record Command(
        Guid AgentId,
        string DisplayName,
        string Description,
        string? SystemPrompt,
        string? Objective,
        string? Capabilities,
        string? TargetPersona,
        string? Icon,
        Guid? PreferredModelId,
        string? AllowedModelIds,
        string? AllowedTools,
        string? InputSchema,
        string? OutputSchema,
        string? Visibility,
        bool? AllowModelOverride,
        int? SortOrder) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de atualização.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.AgentId).NotEmpty();
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.SystemPrompt).MaximumLength(10000);
            RuleFor(x => x.InputSchema).MaximumLength(5000);
            RuleFor(x => x.OutputSchema).MaximumLength(5000);
        }
    }

    /// <summary>Handler que atualiza a definição de um agent customizado.</summary>
    public sealed class Handler(
        IAiAgentRepository agentRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var agent = await agentRepository.GetByIdAsync(
                AiAgentId.From(request.AgentId), cancellationToken);

            if (agent is null)
                return AiGovernanceErrors.AgentNotFound(request.AgentId.ToString());

            AgentVisibility? visibility = request.Visibility is not null
                ? Enum.Parse<AgentVisibility>(request.Visibility, ignoreCase: true)
                : null;

            var updateResult = agent.UpdateDefinition(
                request.DisplayName,
                request.Description,
                request.SystemPrompt,
                request.Objective,
                request.Capabilities,
                request.TargetPersona,
                request.Icon,
                request.PreferredModelId,
                request.AllowedModelIds,
                request.AllowedTools,
                request.InputSchema,
                request.OutputSchema,
                visibility,
                request.AllowModelOverride,
                request.SortOrder);

            if (updateResult.IsFailure)
                return updateResult.Error;

            await agentRepository.UpdateAsync(agent, cancellationToken);

            return new Response(agent.Id.Value);
        }
    }

    /// <summary>Resposta da atualização de agent.</summary>
    public sealed record Response(Guid AgentId);
}
