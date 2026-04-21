using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Application.Features.GetAgentQueryAuditLog;

/// <summary>
/// Feature: GetAgentQueryAuditLog — retorna o histórico de queries de agentes para auditoria.
/// Wave D.4 — Agent-to-Agent Protocol.
/// </summary>
public static class GetAgentQueryAuditLog
{
    public sealed record Query(
        Guid? TokenId = null,
        int HoursBack = 24,
        int Limit = 100) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.HoursBack).InclusiveBetween(1, 720);
            RuleFor(x => x.Limit).InclusiveBetween(1, 500);
        }
    }

    public sealed class Handler(
        IAgentQueryRepository repository,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;
            var since = now.AddHours(-request.HoursBack);

            var records = request.TokenId.HasValue
                ? await repository.ListByTokenAsync(request.TokenId.Value, request.Limit, cancellationToken)
                : await repository.ListByTenantAsync(currentTenant.Id, since, request.Limit, cancellationToken);

            var items = records.Select(r => new AuditItem(
                RecordId: r.Id.Value,
                TokenId: r.TokenId,
                QueryType: r.QueryType,
                ResponseCode: r.ResponseCode,
                DurationMs: r.DurationMs,
                ExecutedAt: r.ExecutedAt,
                ErrorMessage: r.ErrorMessage)).ToList();

            return Result<Response>.Success(new Response(items, since, now));
        }
    }

    public sealed record AuditItem(
        Guid RecordId,
        Guid TokenId,
        string QueryType,
        int ResponseCode,
        long DurationMs,
        DateTimeOffset ExecutedAt,
        string? ErrorMessage);

    public sealed record Response(
        IReadOnlyList<AuditItem> Items,
        DateTimeOffset From,
        DateTimeOffset To);
}
