using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.CreatePolicy;

/// <summary>
/// Feature: CreatePolicy — cria uma nova política de acesso de IA.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class CreatePolicy
{
    /// <summary>Comando de criação de uma política de acesso de IA.</summary>
    public sealed record Command(
        string Name,
        string Description,
        string Scope,
        string ScopeValue,
        bool AllowExternalAI,
        bool InternalOnly,
        int MaxTokensPerRequest) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de política.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Scope).NotEmpty().MaximumLength(50);
            RuleFor(x => x.ScopeValue).NotEmpty().MaximumLength(200);
            RuleFor(x => x.MaxTokensPerRequest).GreaterThan(0);
        }
    }

    /// <summary>Handler que cria uma nova política de acesso de IA.</summary>
    public sealed class Handler(
        IAiAccessPolicyRepository policyRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var policy = AIAccessPolicy.Create(
                request.Name,
                request.Description,
                request.Scope,
                request.ScopeValue,
                request.AllowExternalAI,
                request.InternalOnly,
                request.MaxTokensPerRequest,
                dateTimeProvider.UtcNow);

            await policyRepository.AddAsync(policy, cancellationToken);

            return new Response(policy.Id.Value);
        }
    }

    /// <summary>Resposta da criação da política de acesso de IA.</summary>
    public sealed record Response(Guid PolicyId);
}
