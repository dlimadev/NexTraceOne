using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetGuardrail;

/// <summary>
/// Feature: GetGuardrail — obtém detalhes completos de um guardrail pelo identificador.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class GetGuardrail
{
    /// <summary>Query de consulta de um guardrail pelo identificador.</summary>
    public sealed record Query(Guid GuardrailId) : IQuery<Response>;

    /// <summary>Validador da query GetGuardrail.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.GuardrailId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém os detalhes completos de um guardrail.</summary>
    public sealed class Handler(
        IAiGuardrailRepository guardrailRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var guardrail = await guardrailRepository.GetByIdAsync(
                AiGuardrailId.From(request.GuardrailId), cancellationToken);

            if (guardrail is null)
                return AiGovernanceErrors.GuardrailNotFound(request.GuardrailId.ToString());

            return new Response(
                guardrail.Id.Value, guardrail.Name, guardrail.DisplayName,
                guardrail.Description, guardrail.Category.ToString(), guardrail.GuardType.ToString(),
                guardrail.Pattern, guardrail.PatternType.ToString(), guardrail.Severity.ToString(),
                guardrail.Action.ToString(), guardrail.UserMessage, guardrail.IsActive,
                guardrail.IsOfficial, guardrail.AgentId, guardrail.ModelId,
                guardrail.Priority);
        }
    }

    /// <summary>Resposta com detalhes completos de um guardrail.</summary>
    public sealed record Response(
        Guid GuardrailId, string Name, string DisplayName, string Description,
        string Category, string GuardType, string Pattern, string PatternType,
        string Severity, string Action, string? UserMessage,
        bool IsActive, bool IsOfficial, Guid? AgentId, Guid? ModelId, int Priority);
}
