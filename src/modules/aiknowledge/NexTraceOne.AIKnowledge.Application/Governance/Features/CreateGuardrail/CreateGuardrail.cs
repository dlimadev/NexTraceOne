using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.CreateGuardrail;

/// <summary>
/// Feature: CreateGuardrail — cria um novo guardrail de proteção de input/output.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class CreateGuardrail
{
    /// <summary>Comando de criação de um guardrail de IA.</summary>
    public sealed record Command(
        string Name,
        string DisplayName,
        string Description,
        string Category,
        string GuardType,
        string Pattern,
        string PatternType,
        string Severity,
        string Action,
        string? UserMessage,
        Guid? AgentId,
        Guid? ModelId,
        int Priority) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de guardrail.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
            RuleFor(x => x.GuardType).NotEmpty().Must(t => t is "input" or "output" or "both")
                .WithMessage("GuardType must be 'input', 'output', or 'both'.");
            RuleFor(x => x.Pattern).NotEmpty();
            RuleFor(x => x.PatternType).NotEmpty().Must(t => t is "regex" or "keyword" or "classifier" or "semantic")
                .WithMessage("PatternType must be 'regex', 'keyword', 'classifier', or 'semantic'.");
            RuleFor(x => x.Severity).NotEmpty().Must(s => s is "critical" or "high" or "medium" or "low" or "info")
                .WithMessage("Severity must be 'critical', 'high', 'medium', 'low', or 'info'.");
            RuleFor(x => x.Action).NotEmpty().Must(a => a is "block" or "sanitize" or "warn" or "log")
                .WithMessage("Action must be 'block', 'sanitize', 'warn', or 'log'.");
            RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
        }
    }

    /// <summary>Handler que cria um novo guardrail de IA.</summary>
    public sealed class Handler(
        IAiGuardrailRepository guardrailRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (await guardrailRepository.ExistsByNameAsync(request.Name, cancellationToken))
                return AiGovernanceErrors.GuardrailDuplicateName(request.Name);

            var guardrail = AiGuardrail.Create(
                name: request.Name,
                displayName: request.DisplayName,
                description: request.Description,
                category: request.Category,
                guardType: request.GuardType,
                pattern: request.Pattern,
                patternType: request.PatternType,
                severity: request.Severity,
                action: request.Action,
                userMessage: request.UserMessage,
                isActive: true,
                isOfficial: false,
                agentId: request.AgentId,
                modelId: request.ModelId,
                priority: request.Priority);

            await guardrailRepository.AddAsync(guardrail, cancellationToken);

            return new Response(guardrail.Id.Value);
        }
    }

    /// <summary>Resposta da criação do guardrail.</summary>
    public sealed record Response(Guid GuardrailId);
}
