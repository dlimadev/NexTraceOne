using Ardalis.GuardClauses;

using FluentValidation;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ConfigureExternalAIPolicy;

/// <summary>
/// Feature: ConfigureExternalAIPolicy — persiste e atualiza política de uso/captura de IA externa.
/// Cria nova política ou atualiza existente (por nome). A política fica persistida e consultável,
/// com efeito real nos fluxos de capture/aprovação/reuso.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ConfigureExternalAIPolicy
{
    // ── COMMAND ───────────────────────────────────────────────────────────

    /// <summary>Comando para criar ou atualizar uma política de IA externa.</summary>
    public sealed record Command(
        string Name,
        string Description,
        int MaxDailyQueries,
        long MaxTokensPerDay,
        bool RequiresApproval,
        string AllowedContexts) : ICommand<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(1_000);
            RuleFor(x => x.MaxDailyQueries).GreaterThan(0);
            RuleFor(x => x.MaxTokensPerDay).GreaterThan(0L);
            RuleFor(x => x.AllowedContexts).NotEmpty().MaximumLength(1_000);
        }
    }

    // ── HANDLER ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IExternalAiPolicyRepository policyRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;
            var existing = await policyRepository.GetByNameAsync(request.Name, cancellationToken);

            bool isNew;
            ExternalAiPolicy policy;

            if (existing is null)
            {
                policy = ExternalAiPolicy.Create(
                    request.Name,
                    request.Description,
                    request.MaxDailyQueries,
                    request.MaxTokensPerDay,
                    request.RequiresApproval,
                    request.AllowedContexts,
                    now);

                await policyRepository.AddAsync(policy, cancellationToken);
                isNew = true;
            }
            else
            {
                var updateResult = existing.Update(
                    request.Description,
                    request.MaxDailyQueries,
                    request.MaxTokensPerDay,
                    request.RequiresApproval,
                    request.AllowedContexts);

                if (!updateResult.IsSuccess)
                    return updateResult.Error!;

                await policyRepository.UpdateAsync(existing, cancellationToken);
                policy = existing;
                isNew = false;
            }

            logger.LogInformation(
                "External AI policy '{PolicyName}' {Action}. MaxDailyQueries={MaxDailyQueries}, RequiresApproval={RequiresApproval}",
                request.Name, isNew ? "created" : "updated", request.MaxDailyQueries, request.RequiresApproval);

            return new Response(
                policy.Id.Value,
                policy.Name,
                policy.Description,
                policy.MaxDailyQueries,
                policy.MaxTokensPerDay,
                policy.RequiresApproval,
                policy.AllowedContexts,
                policy.IsActive,
                isNew ? "Created" : "Updated");
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    /// <summary>Resultado da configuração da política de IA externa.</summary>
    public sealed record Response(
        Guid PolicyId,
        string Name,
        string Description,
        int MaxDailyQueries,
        long MaxTokensPerDay,
        bool RequiresApproval,
        string AllowedContexts,
        bool IsActive,
        string Action);
}
