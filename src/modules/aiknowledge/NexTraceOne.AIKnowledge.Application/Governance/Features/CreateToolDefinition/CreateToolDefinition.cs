using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.CreateToolDefinition;

/// <summary>
/// Feature: CreateToolDefinition — regista uma nova definição de ferramenta de IA.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class CreateToolDefinition
{
    /// <summary>Comando de criação de uma definição de ferramenta.</summary>
    public sealed record Command(
        string Name,
        string DisplayName,
        string Description,
        string Category,
        string ParametersSchema,
        bool RequiresApproval,
        int RiskLevel,
        int TimeoutMs) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de ferramenta.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
            RuleFor(x => x.RiskLevel).InclusiveBetween(0, 3);
            RuleFor(x => x.TimeoutMs).GreaterThan(0);
        }
    }

    /// <summary>Handler que cria uma nova definição de ferramenta.</summary>
    public sealed class Handler(
        IAiToolDefinitionRepository toolRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (await toolRepository.ExistsByNameAsync(request.Name, cancellationToken))
                return AiGovernanceErrors.ToolDefinitionDuplicateName(request.Name);

            var tool = AiToolDefinition.Create(
                name: request.Name,
                displayName: request.DisplayName,
                description: request.Description,
                category: request.Category,
                parametersSchema: request.ParametersSchema ?? "{}",
                version: 1,
                isActive: true,
                requiresApproval: request.RequiresApproval,
                riskLevel: request.RiskLevel,
                isOfficial: false,
                timeoutMs: request.TimeoutMs > 0 ? request.TimeoutMs : 30000);

            await toolRepository.AddAsync(tool, cancellationToken);

            return new Response(tool.Id.Value);
        }
    }

    /// <summary>Resposta da criação da definição de ferramenta.</summary>
    public sealed record Response(Guid ToolId);
}
