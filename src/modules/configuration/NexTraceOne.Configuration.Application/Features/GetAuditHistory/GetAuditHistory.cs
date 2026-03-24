using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;

namespace NexTraceOne.Configuration.Application.Features.GetAuditHistory;

/// <summary>
/// Feature: GetAuditHistory — returns audit trail entries for a configuration key.
/// Sensitive values are masked in the response.
/// </summary>
public static class GetAuditHistory
{
    /// <summary>Query to retrieve audit history for a given key with configurable limit.</summary>
    public sealed record Query(
        string Key,
        int Limit = 50) : IQuery<List<ConfigurationAuditEntryDto>>;

    /// <summary>Handler that fetches audit entries and masks sensitive values.</summary>
    public sealed class Handler(
        IConfigurationAuditRepository auditRepository,
        IConfigurationSecurityService securityService)
        : IQueryHandler<Query, List<ConfigurationAuditEntryDto>>
    {
        public async Task<Result<List<ConfigurationAuditEntryDto>>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var limit = request.Limit > 0 ? request.Limit : 50;
            var entries = await auditRepository.GetByKeyAsync(request.Key, limit, cancellationToken);

            var dtos = entries.Select(e => new ConfigurationAuditEntryDto(
                Key: e.Key,
                Scope: e.Scope.ToString(),
                ScopeReferenceId: e.ScopeReferenceId,
                Action: e.Action,
                PreviousValue: e.IsSensitive && e.PreviousValue is not null
                    ? securityService.MaskValue(e.PreviousValue)
                    : e.PreviousValue,
                NewValue: e.IsSensitive && e.NewValue is not null
                    ? securityService.MaskValue(e.NewValue)
                    : e.NewValue,
                PreviousVersion: e.PreviousVersion,
                NewVersion: e.NewVersion,
                ChangedBy: e.ChangedBy,
                ChangedAt: e.ChangedAt,
                ChangeReason: e.ChangeReason,
                IsSensitive: e.IsSensitive)).ToList();

            return dtos;
        }
    }
}
