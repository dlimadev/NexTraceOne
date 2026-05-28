using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.CreateUserTokenQuota;

/// <summary>
/// Feature: CreateUserTokenQuota — cria uma política de quota de tokens para um utilizador específico.
/// Define limites por pedido, diários e mensais por modelo ou provider.
/// </summary>
public static class CreateUserTokenQuota
{
    /// <summary>Comando de criação de quota de tokens por utilizador.</summary>
    public sealed record Command(
        string UserId,
        string? ProviderId,
        string? ModelId,
        int MaxInputTokensPerRequest,
        int MaxOutputTokensPerRequest,
        long MaxTokensPerDay,
        long MaxTokensPerMonth,
        bool IsHardLimit) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.MaxInputTokensPerRequest).GreaterThan(0);
            RuleFor(x => x.MaxOutputTokensPerRequest).GreaterThan(0);
            RuleFor(x => x.MaxTokensPerDay).GreaterThan(0);
            RuleFor(x => x.MaxTokensPerMonth).GreaterThan(0);
            RuleFor(x => x.MaxTokensPerMonth)
                .GreaterThanOrEqualTo(x => x.MaxTokensPerDay)
                .WithMessage("O limite mensal deve ser maior ou igual ao diário.");
        }
    }

    /// <summary>Handler que cria a quota de tokens para o utilizador.</summary>
    public sealed class Handler(
        IAiTokenQuotaPolicyRepository quotaRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var maxTotal = request.MaxInputTokensPerRequest + request.MaxOutputTokensPerRequest;

            var quota = AiTokenQuotaPolicy.Create(
                name: $"user-quota:{request.UserId}{(request.ModelId is not null ? $":{request.ModelId}" : "")}",
                description: $"Quota de tokens para utilizador {request.UserId}",
                scope: "user",
                scopeValue: request.UserId,
                providerId: request.ProviderId,
                modelId: request.ModelId,
                maxInputTokensPerRequest: request.MaxInputTokensPerRequest,
                maxOutputTokensPerRequest: request.MaxOutputTokensPerRequest,
                maxTotalTokensPerRequest: maxTotal,
                maxTokensPerDay: request.MaxTokensPerDay,
                maxTokensPerMonth: request.MaxTokensPerMonth,
                maxTokensAccumulated: request.MaxTokensPerMonth * 12,
                isHardLimit: request.IsHardLimit,
                allowSensitiveData: false,
                allowKnowledgePromotion: false);

            await quotaRepository.AddAsync(quota, cancellationToken);

            return new Response(quota.Id.Value, request.UserId);
        }
    }

    /// <summary>Resposta da criação da quota de tokens por utilizador.</summary>
    public sealed record Response(Guid QuotaId, string UserId);
}
