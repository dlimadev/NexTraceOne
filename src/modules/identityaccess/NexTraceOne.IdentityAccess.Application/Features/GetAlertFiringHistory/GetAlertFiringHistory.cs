using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.GetAlertFiringHistory;

/// <summary>
/// SaaS-08: Retorna o histórico de alertas disparados para o tenant corrente.
/// </summary>
public static class GetAlertFiringHistory
{
    public sealed record Query(
        string? StatusFilter,
        int Days = 30) : IQuery<Response>;

    public sealed record AlertFiringDto(
        Guid RecordId,
        Guid AlertRuleId,
        string AlertRuleName,
        string Severity,
        string ConditionSummary,
        string? ServiceName,
        string Status,
        DateTimeOffset FiredAt,
        DateTimeOffset? ResolvedAt,
        string? ResolvedReason);

    public sealed record Response(
        IReadOnlyList<AlertFiringDto> Records,
        int TotalFiring,
        int TotalResolved,
        int TotalSilenced);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Days).InclusiveBetween(1, 90);
        }
    }

    public sealed class Handler(
        IAlertFiringRecordRepository repository,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            var tenantId = currentTenant.TenantId;

            AlertFiringStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(request.StatusFilter) &&
                Enum.TryParse<AlertFiringStatus>(request.StatusFilter, ignoreCase: true, out var parsed))
            {
                statusFilter = parsed;
            }

            var records = await repository.ListByTenantAsync(tenantId, statusFilter, request.Days, cancellationToken);

            var dtos = records.Select(r => new AlertFiringDto(
                r.Id.Value,
                r.AlertRuleId,
                r.AlertRuleName,
                r.Severity,
                r.ConditionSummary,
                r.ServiceName,
                r.Status.ToString(),
                r.FiredAt,
                r.ResolvedAt,
                r.ResolvedReason)).ToList();

            return Result.Success(new Response(
                dtos,
                records.Count(r => r.Status == AlertFiringStatus.Firing),
                records.Count(r => r.Status == AlertFiringStatus.Resolved),
                records.Count(r => r.Status == AlertFiringStatus.Silenced)));
        }
    }
}
