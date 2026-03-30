using Ardalis.GuardClauses;

using FluentValidation;

using MediatR;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateGuardrail;

/// <summary>
/// Feature: UpdateGuardrail — atualiza estado (ativar/desativar) de um guardrail.
/// Estrutura VSA: Command + Validator + Handler num único ficheiro.
/// </summary>
public static class UpdateGuardrail
{
    /// <summary>Comando de atualização de estado de um guardrail.</summary>
    public sealed record Command(
        Guid GuardrailId,
        bool? IsActive) : ICommand;

    /// <summary>Valida a entrada do comando de atualização de guardrail.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.GuardrailId).NotEmpty();
        }
    }

    /// <summary>Handler que atualiza estado de um guardrail.</summary>
    public sealed class Handler(
        IAiGuardrailRepository guardrailRepository) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var guardrail = await guardrailRepository.GetByIdAsync(
                AiGuardrailId.From(request.GuardrailId), cancellationToken);

            if (guardrail is null)
                return AiGovernanceErrors.GuardrailNotFound(request.GuardrailId.ToString());

            if (request.IsActive.HasValue)
            {
                if (request.IsActive.Value)
                    guardrail.Activate();
                else
                    guardrail.Deactivate();
            }

            return Unit.Value;
        }
    }
}
