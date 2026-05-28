using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.CreateUserModelPolicy;

/// <summary>
/// Feature: CreateUserModelPolicy — cria uma política de acesso a modelos para um utilizador específico.
/// Define quais modelos um utilizador pode usar (allowlist) e quais estão bloqueados (denylist).
/// Internamente usa AIAccessPolicy com Scope = "user".
/// </summary>
public static class CreateUserModelPolicy
{
    /// <summary>Comando de criação de política de modelos por utilizador.</summary>
    public sealed record Command(
        string UserId,
        string UserDisplayName,
        string AllowedModelIds,
        string BlockedModelIds,
        bool AllowExternalAI,
        bool InternalOnly,
        int MaxTokensPerRequest) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.UserDisplayName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.MaxTokensPerRequest).GreaterThan(0);
        }
    }

    /// <summary>Handler que cria a política de acesso a modelos para o utilizador.</summary>
    public sealed class Handler(
        IAiAccessPolicyRepository policyRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var policy = AIAccessPolicy.Create(
                name: $"user-model-policy:{request.UserId}",
                description: $"Política de acesso a modelos para o utilizador {request.UserDisplayName}",
                scope: "user",
                scopeValue: request.UserId,
                allowExternalAI: request.AllowExternalAI,
                internalOnly: request.InternalOnly,
                maxTokensPerRequest: request.MaxTokensPerRequest,
                createdAt: dateTimeProvider.UtcNow);

            if (!string.IsNullOrWhiteSpace(request.AllowedModelIds))
                policy.SetAllowedModels(request.AllowedModelIds);

            if (!string.IsNullOrWhiteSpace(request.BlockedModelIds))
                policy.SetBlockedModels(request.BlockedModelIds);

            await policyRepository.AddAsync(policy, cancellationToken);

            return new Response(policy.Id.Value, request.UserId);
        }
    }

    /// <summary>Resposta da criação da política de modelos por utilizador.</summary>
    public sealed record Response(Guid PolicyId, string UserId);
}
