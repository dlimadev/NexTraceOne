using Ardalis.GuardClauses;

using FluentValidation;

using MediatR;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.UpdatePromptTemplate;

/// <summary>
/// Feature: UpdatePromptTemplate — atualiza estado (ativar/desativar) de um template.
/// Estrutura VSA: Command + Validator + Handler num único ficheiro.
/// </summary>
public static class UpdatePromptTemplate
{
    /// <summary>Comando de atualização de estado de um template de prompt.</summary>
    public sealed record Command(
        Guid TemplateId,
        bool? IsActive) : ICommand;

    /// <summary>Valida a entrada do comando de atualização de template.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TemplateId).NotEmpty();
        }
    }

    /// <summary>Handler que atualiza estado de um template de prompt.</summary>
    public sealed class Handler(
        IPromptTemplateRepository templateRepository) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var template = await templateRepository.GetByIdAsync(
                PromptTemplateId.From(request.TemplateId), cancellationToken);

            if (template is null)
                return AiGovernanceErrors.PromptTemplateNotFound(request.TemplateId.ToString());

            if (request.IsActive.HasValue)
            {
                if (request.IsActive.Value)
                    template.Activate();
                else
                    template.Deactivate();
            }

            return Unit.Value;
        }
    }
}
