using Ardalis.GuardClauses;

using FluentValidation;

using MediatR;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateToolDefinition;

/// <summary>
/// Feature: UpdateToolDefinition — atualiza estado (ativar/desativar) de uma ferramenta.
/// Estrutura VSA: Command + Validator + Handler num único ficheiro.
/// </summary>
public static class UpdateToolDefinition
{
    /// <summary>Comando de atualização de estado de uma definição de ferramenta.</summary>
    public sealed record Command(
        Guid ToolId,
        bool? IsActive) : ICommand;

    /// <summary>Valida a entrada do comando de atualização de ferramenta.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ToolId).NotEmpty();
        }
    }

    /// <summary>Handler que atualiza estado de uma definição de ferramenta.</summary>
    public sealed class Handler(
        IAiToolDefinitionRepository toolRepository) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var tool = await toolRepository.GetByIdAsync(
                AiToolDefinitionId.From(request.ToolId), cancellationToken);

            if (tool is null)
                return AiGovernanceErrors.ToolDefinitionNotFound(request.ToolId.ToString());

            if (request.IsActive.HasValue)
            {
                if (request.IsActive.Value)
                    tool.Activate();
                else
                    tool.Deactivate();
            }

            return Unit.Value;
        }
    }
}
