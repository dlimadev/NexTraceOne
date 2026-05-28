using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListUserTokenQuotas;

/// <summary>
/// Feature: ListUserTokenQuotas — lista quotas de tokens por utilizador (scope = "user").
/// </summary>
public static class ListUserTokenQuotas
{
    /// <summary>Query de listagem de quotas por utilizador, com filtro opcional por userId.</summary>
    public sealed record Query(string? UserId) : IQuery<Response>;

    /// <summary>Handler que lista quotas de todos os utilizadores ou de um específico.</summary>
    public sealed class Handler(
        IAiTokenQuotaPolicyRepository quotaRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            IReadOnlyList<AiTokenQuotaPolicy> quotas;

            if (!string.IsNullOrWhiteSpace(request.UserId))
                quotas = await quotaRepository.GetForUserAsync(request.UserId, cancellationToken);
            else
                quotas = await quotaRepository.GetByScopeAsync("user", cancellationToken);

            var items = quotas.Select(q => new UserTokenQuotaDto(
                q.Id.Value,
                q.ScopeValue,
                q.Name,
                q.ProviderId,
                q.ModelId,
                q.MaxInputTokensPerRequest,
                q.MaxOutputTokensPerRequest,
                q.MaxTotalTokensPerRequest,
                q.MaxTokensPerDay,
                q.MaxTokensPerMonth,
                q.IsHardLimit,
                q.IsEnabled,
                q.CreatedAt,
                q.UpdatedAt)).ToList();

            return new Response(items);
        }
    }

    /// <summary>Resposta da listagem de quotas por utilizador.</summary>
    public sealed record Response(IReadOnlyList<UserTokenQuotaDto> Items);

    /// <summary>DTO de quota de tokens por utilizador.</summary>
    public sealed record UserTokenQuotaDto(
        Guid QuotaId,
        string UserId,
        string PolicyName,
        string? ProviderId,
        string? ModelId,
        int MaxInputTokensPerRequest,
        int MaxOutputTokensPerRequest,
        int MaxTotalTokensPerRequest,
        long MaxTokensPerDay,
        long MaxTokensPerMonth,
        bool IsHardLimit,
        bool IsEnabled,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);
}
