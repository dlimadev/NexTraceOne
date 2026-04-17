using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.GetTenant;

/// <summary>
/// Feature: GetTenant — obtém os detalhes completos de um tenant pelo seu Id.
/// Uso exclusivo de Platform Admin.
/// </summary>
public static class GetTenant
{
    /// <summary>Query por Id do tenant.</summary>
    public sealed record Query(Guid TenantId) : IQuery<TenantDetail>;

    /// <summary>Detalhe completo de um tenant.</summary>
    public sealed record TenantDetail(
        Guid Id,
        string Name,
        string Slug,
        bool IsActive,
        string TenantType,
        string? LegalName,
        string? TaxId,
        Guid? ParentTenantId,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt);

    /// <summary>Handler que obtém o tenant pelo Id.</summary>
    public sealed class Handler(
        ITenantRepository tenantRepository) : IQueryHandler<Query, TenantDetail>
    {
        public async Task<Result<TenantDetail>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var tenant = await tenantRepository.GetByIdAsync(TenantId.From(request.TenantId), cancellationToken);
            if (tenant is null)
                return IdentityErrors.TenantNotFound(request.TenantId);

            return new TenantDetail(
                tenant.Id.Value,
                tenant.Name,
                tenant.Slug,
                tenant.IsActive,
                tenant.TenantType.ToString(),
                tenant.LegalName,
                tenant.TaxId,
                tenant.ParentTenantId?.Value,
                tenant.CreatedAt,
                tenant.UpdatedAt);
        }
    }
}
