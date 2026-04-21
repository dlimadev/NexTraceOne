using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.ListPlatformApiTokens;

/// <summary>
/// Feature: ListPlatformApiTokens — lista os tokens de acesso de plataforma do tenant actual.
/// Wave D.4 — Agent-to-Agent Protocol.
/// </summary>
public static class ListPlatformApiTokens
{
    public sealed record Query : IQuery<Response>;

    public sealed class Handler(
        IPlatformApiTokenRepository repository,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var tokens = await repository.ListByTenantAsync(currentTenant.Id, cancellationToken);
            var now = clock.UtcNow;

            var items = tokens.Select(t => new TokenItem(
                TokenId: t.Id.Value,
                Name: t.Name,
                TokenPrefix: t.TokenPrefix,
                Scope: t.Scope,
                IsActive: t.IsActive(now),
                CreatedAt: t.CreatedAt,
                ExpiresAt: t.ExpiresAt,
                LastUsedAt: t.LastUsedAt)).ToList();

            return Result<Response>.Success(new Response(items));
        }
    }

    public sealed record TokenItem(
        Guid TokenId,
        string Name,
        string TokenPrefix,
        PlatformApiTokenScope Scope,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ExpiresAt,
        DateTimeOffset? LastUsedAt);

    public sealed record Response(IReadOnlyList<TokenItem> Tokens);
}
