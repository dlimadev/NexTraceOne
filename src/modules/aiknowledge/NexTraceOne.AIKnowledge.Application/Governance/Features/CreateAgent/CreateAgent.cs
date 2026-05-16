using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.CreateAgent;

/// <summary>
/// Feature: CreateAgent — cria um agent customizado (Tenant ou User).
/// Agents System são criados via seed/migração e não via API.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class CreateAgent
{
    /// <summary>Comando de criação de um agent customizado.</summary>
    public sealed record Command(
        string Name,
        string DisplayName,
        string Description,
        string Category,
        string SystemPrompt,
        string Objective,
        string OwnershipType,
        string Visibility,
        Guid? PreferredModelId,
        string? AllowedModelIds,
        string? AllowedTools,
        string? Capabilities,
        string? TargetPersona,
        string? InputSchema,
        string? OutputSchema,
        string? Icon,
        bool AllowModelOverride = true) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de agent.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Category).NotEmpty();
            RuleFor(x => x.SystemPrompt).NotEmpty().MaximumLength(10000);
            RuleFor(x => x.Objective).MaximumLength(2000);
            RuleFor(x => x.OwnershipType).NotEmpty()
                .Must(v => v != "System")
                .WithMessage("System agents cannot be created via API.");
            RuleFor(x => x.Visibility).NotEmpty();
            RuleFor(x => x.InputSchema).MaximumLength(5000);
            RuleFor(x => x.OutputSchema).MaximumLength(5000);
        }
    }

    /// <summary>Handler que cria um agent customizado.</summary>
    public sealed class Handler(
        IAiAgentRepository agentRepository,
        ICurrentUser currentUser) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!Enum.TryParse<AgentCategory>(request.Category, ignoreCase: true, out var category))
                return Error.Validation("Agent.InvalidCategory", $"'{request.Category}' is not a valid agent category.");
            if (!Enum.TryParse<AgentOwnershipType>(request.OwnershipType, ignoreCase: true, out var ownershipType))
                return Error.Validation("Agent.InvalidOwnershipType", $"'{request.OwnershipType}' is not a valid agent ownership type.");
            if (!Enum.TryParse<AgentVisibility>(request.Visibility, ignoreCase: true, out var visibility))
                return Error.Validation("Agent.InvalidVisibility", $"'{request.Visibility}' is not a valid agent visibility.");

            var exists = await agentRepository.ExistsByNameAsync(request.Name, cancellationToken);
            if (exists)
                return Error.Conflict("Agent.NameAlreadyExists", $"An agent with name '{request.Name}' already exists.");

            var agent = AiAgent.CreateCustom(
                request.Name,
                request.DisplayName,
                request.Description,
                category,
                request.SystemPrompt,
                request.Objective,
                ownershipType,
                visibility,
                currentUser.Id,
                ownerTeamId: null,
                preferredModelId: request.PreferredModelId,
                allowedModelIds: request.AllowedModelIds,
                allowedTools: request.AllowedTools,
                capabilities: request.Capabilities,
                targetPersona: request.TargetPersona,
                inputSchema: request.InputSchema,
                outputSchema: request.OutputSchema,
                icon: request.Icon,
                allowModelOverride: request.AllowModelOverride);

            await agentRepository.AddAsync(agent, cancellationToken);

            return new Response(agent.Id.Value, agent.Slug);
        }
    }

    /// <summary>Resposta da criação de agent.</summary>
    public sealed record Response(Guid AgentId, string Slug);
}
