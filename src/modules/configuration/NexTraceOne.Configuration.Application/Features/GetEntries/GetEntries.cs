using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Features.GetEntries;

/// <summary>
/// Feature: GetEntries — returns configuration entries for a given scope.
/// Sensitive values are masked before being returned.
/// </summary>
public static class GetEntries
{
    /// <summary>Query to retrieve entries by scope and optional scope reference.</summary>
    public sealed record Query(
        ConfigurationScope Scope,
        string? ScopeReferenceId) : IQuery<List<ConfigurationEntryDto>>;

    /// <summary>Handler that fetches entries and masks sensitive values.</summary>
    public sealed class Handler(
        IConfigurationEntryRepository entryRepository,
        IConfigurationSecurityService securityService)
        : IQueryHandler<Query, List<ConfigurationEntryDto>>
    {
        public async Task<Result<List<ConfigurationEntryDto>>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var entries = await entryRepository.GetAllByScopeAsync(
                request.Scope,
                request.ScopeReferenceId,
                cancellationToken);

            var dtos = entries.Select(e => new ConfigurationEntryDto(
                Id: e.Id.Value,
                DefinitionKey: e.Key,
                Scope: e.Scope.ToString(),
                ScopeReferenceId: Guid.TryParse(e.ScopeReferenceId, out var refId) ? refId : null,
                Value: e.IsSensitive ? securityService.MaskValue(e.Value ?? string.Empty) : e.Value,
                IsActive: e.IsActive,
                Version: e.Version,
                ChangeReason: e.ChangeReason,
                UpdatedAt: e.UpdatedAt ?? e.CreatedAt,
                UpdatedBy: e.UpdatedBy ?? e.CreatedBy)).ToList();

            return dtos;
        }
    }
}
