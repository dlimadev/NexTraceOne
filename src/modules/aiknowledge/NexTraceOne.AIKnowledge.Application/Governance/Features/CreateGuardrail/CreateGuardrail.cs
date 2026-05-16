using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.CreateGuardrail;

/// <summary>
/// Feature: CreateGuardrail — cria um novo guardrail de proteção de input/output.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// Aceita strings na API (compatibilidade) e converte para enums fortemente tipados internamente. (E-M01)
/// </summary>
public static class CreateGuardrail
{
    /// <summary>Comando de criação de um guardrail de IA. Strings são validadas e convertidas para enums.</summary>
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

    /// <summary>Valida a entrada do comando de criação de guardrail. (E-M01)</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] ValidCategories = ["Security", "Privacy", "Compliance", "Quality",
            "security", "privacy", "compliance", "quality"];
        private static readonly string[] ValidGuardTypes = ["Input", "Output", "Both", "input", "output", "both"];
        private static readonly string[] ValidPatternTypes = ["Regex", "Keyword", "Classifier", "Semantic",
            "regex", "keyword", "classifier", "semantic"];
        private static readonly string[] ValidSeverities = ["Critical", "High", "Medium", "Low", "Info",
            "critical", "high", "medium", "low", "info"];
        private static readonly string[] ValidActions = ["Block", "Sanitize", "Warn", "Log",
            "block", "sanitize", "warn", "log"];

        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Category).NotEmpty().MaximumLength(100)
                .Must(c => Enum.TryParse<GuardrailCategory>(c, ignoreCase: true, out _))
                .WithMessage($"Category must be one of: {string.Join(", ", Enum.GetNames<GuardrailCategory>())}.");
            RuleFor(x => x.GuardType).NotEmpty()
                .Must(t => Enum.TryParse<GuardrailType>(t, ignoreCase: true, out _))
                .WithMessage($"GuardType must be one of: {string.Join(", ", Enum.GetNames<GuardrailType>())}.");
            RuleFor(x => x.Pattern).NotEmpty();
            RuleFor(x => x.PatternType).NotEmpty()
                .Must(t => Enum.TryParse<GuardrailPatternType>(t, ignoreCase: true, out _))
                .WithMessage($"PatternType must be one of: {string.Join(", ", Enum.GetNames<GuardrailPatternType>())}.");
            RuleFor(x => x.Severity).NotEmpty()
                .Must(s => Enum.TryParse<GuardrailSeverity>(s, ignoreCase: true, out _))
                .WithMessage($"Severity must be one of: {string.Join(", ", Enum.GetNames<GuardrailSeverity>())}.");
            RuleFor(x => x.Action).NotEmpty()
                .Must(a => Enum.TryParse<GuardrailAction>(a, ignoreCase: true, out _))
                .WithMessage($"Action must be one of: {string.Join(", ", Enum.GetNames<GuardrailAction>())}.");
            RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
        }
    }

    /// <summary>Handler que cria um novo guardrail de IA com campos de enum fortemente tipados.</summary>
    public sealed class Handler(
        IAiGuardrailRepository guardrailRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (await guardrailRepository.ExistsByNameAsync(request.Name, cancellationToken))
                return AiGovernanceErrors.GuardrailDuplicateName(request.Name);

            if (!Enum.TryParse<GuardrailCategory>(request.Category, ignoreCase: true, out var category))
                return Error.Validation("Guardrail.InvalidCategory", $"'{request.Category}' is not a valid guardrail category.");
            if (!Enum.TryParse<GuardrailType>(request.GuardType, ignoreCase: true, out var guardType))
                return Error.Validation("Guardrail.InvalidGuardType", $"'{request.GuardType}' is not a valid guardrail type.");
            if (!Enum.TryParse<GuardrailPatternType>(request.PatternType, ignoreCase: true, out var patternType))
                return Error.Validation("Guardrail.InvalidPatternType", $"'{request.PatternType}' is not a valid guardrail pattern type.");
            if (!Enum.TryParse<GuardrailSeverity>(request.Severity, ignoreCase: true, out var severity))
                return Error.Validation("Guardrail.InvalidSeverity", $"'{request.Severity}' is not a valid guardrail severity.");
            if (!Enum.TryParse<GuardrailAction>(request.Action, ignoreCase: true, out var action))
                return Error.Validation("Guardrail.InvalidAction", $"'{request.Action}' is not a valid guardrail action.");

            var guardrail = AiGuardrail.Create(
                name: request.Name,
                displayName: request.DisplayName,
                description: request.Description,
                category: category,
                guardType: guardType,
                pattern: request.Pattern,
                patternType: patternType,
                severity: severity,
                action: action,
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
