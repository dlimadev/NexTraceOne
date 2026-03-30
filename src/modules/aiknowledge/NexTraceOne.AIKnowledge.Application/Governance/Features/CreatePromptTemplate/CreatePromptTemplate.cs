using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.CreatePromptTemplate;

/// <summary>
/// Feature: CreatePromptTemplate — cria um novo template de prompt versionado.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class CreatePromptTemplate
{
    /// <summary>Comando de criação de um template de prompt.</summary>
    public sealed record Command(
        string Name,
        string DisplayName,
        string Description,
        string Category,
        string Content,
        string Variables,
        string TargetPersonas,
        string? ScopeHint,
        string Relevance,
        Guid? PreferredModelId,
        decimal? RecommendedTemperature,
        int? MaxOutputTokens) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de template.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Content).NotEmpty();
            RuleFor(x => x.RecommendedTemperature).InclusiveBetween(0m, 2m)
                .When(x => x.RecommendedTemperature.HasValue);
            RuleFor(x => x.MaxOutputTokens).GreaterThan(0)
                .When(x => x.MaxOutputTokens.HasValue);
        }
    }

    /// <summary>Handler que cria um novo template de prompt.</summary>
    public sealed class Handler(
        IPromptTemplateRepository templateRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (await templateRepository.ExistsByNameAsync(request.Name, cancellationToken))
                return AiGovernanceErrors.PromptTemplateDuplicateName(request.Name);

            var template = PromptTemplate.Create(
                name: request.Name,
                displayName: request.DisplayName,
                description: request.Description,
                category: request.Category,
                content: request.Content,
                variables: request.Variables ?? string.Empty,
                version: 1,
                isActive: true,
                isOfficial: false,
                agentId: null,
                targetPersonas: request.TargetPersonas ?? string.Empty,
                scopeHint: request.ScopeHint,
                relevance: request.Relevance ?? "medium",
                preferredModelId: request.PreferredModelId,
                recommendedTemperature: request.RecommendedTemperature,
                maxOutputTokens: request.MaxOutputTokens);

            await templateRepository.AddAsync(template, cancellationToken);

            return new Response(template.Id.Value);
        }
    }

    /// <summary>Resposta da criação do template de prompt.</summary>
    public sealed record Response(Guid TemplateId);
}
