using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateUserTokenQuota;

/// <summary>
/// Feature: UpdateUserTokenQuota — atualiza a quota de tokens de um utilizador.
/// </summary>
public static class UpdateUserTokenQuota
{
    /// <summary>Comando de atualização de quota de tokens.</summary>
    public sealed record Command(
        Guid QuotaId,
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
            RuleFor(x => x.QuotaId).NotEqual(Guid.Empty);
            RuleFor(x => x.MaxInputTokensPerRequest).GreaterThan(0);
            RuleFor(x => x.MaxOutputTokensPerRequest).GreaterThan(0);
            RuleFor(x => x.MaxTokensPerDay).GreaterThan(0);
            RuleFor(x => x.MaxTokensPerMonth).GreaterThan(0);
            RuleFor(x => x.MaxTokensPerMonth)
                .GreaterThanOrEqualTo(x => x.MaxTokensPerDay)
                .WithMessage("O limite mensal deve ser maior ou igual ao diário.");
        }
    }

    /// <summary>Handler que atualiza a quota de tokens.</summary>
    public sealed class Handler(
        IAiTokenQuotaPolicyRepository quotaRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var quota = await quotaRepository.GetByIdAsync(
                AiTokenQuotaPolicyId.From(request.QuotaId), cancellationToken);

            if (quota is null)
                return Error.NotFound(
                    "UserTokenQuota.NotFound",
                    "Quota '{0}' não encontrada.",
                    request.QuotaId);

            var maxTotal = request.MaxInputTokensPerRequest + request.MaxOutputTokensPerRequest;

            quota.Update(
                quota.Description,
                request.MaxInputTokensPerRequest,
                request.MaxOutputTokensPerRequest,
                maxTotal,
                request.MaxTokensPerDay,
                request.MaxTokensPerMonth,
                request.MaxTokensPerMonth * 12,
                request.IsHardLimit,
                quota.AllowSensitiveData,
                quota.AllowKnowledgePromotion);

            return new Response(request.QuotaId);
        }
    }

    /// <summary>Resposta da atualização da quota.</summary>
    public sealed record Response(Guid QuotaId);
}
