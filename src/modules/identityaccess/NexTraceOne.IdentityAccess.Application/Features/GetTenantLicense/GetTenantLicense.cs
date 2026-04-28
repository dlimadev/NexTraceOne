using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.GetTenantLicense;

/// <summary>
/// SaaS-04: Retorna a licença actual do tenant corrente.
/// </summary>
public static class GetTenantLicense
{
    public sealed record Query : IQuery<Response>;

    public sealed record Response(
        Guid LicenseId,
        string Plan,
        string Status,
        int IncludedHostUnits,
        decimal CurrentHostUnits,
        decimal OverageHostUnits,
        DateTimeOffset ValidFrom,
        DateTimeOffset? ValidUntil,
        IReadOnlyList<string> Capabilities);

    public sealed class Handler(
        ITenantLicenseRepository repository,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            var tenantId = currentTenant.Id;

            var license = await repository.GetByTenantIdAsync(tenantId, cancellationToken);
            if (license is null)
            {
                // Tenant sem licença provisionada — retorna Starter trial por defeito
                return Result<Response>.Success(new Response(
                    Guid.Empty,
                    TenantPlan.Starter.ToString(),
                    TenantLicenseStatus.Active.ToString(),
                    10,
                    0m,
                    0m,
                    DateTimeOffset.UtcNow,
                    null,
                    TenantCapabilities.ForPlan(TenantPlan.Starter)));
            }

            return Result<Response>.Success(new Response(
                license.Id.Value,
                license.Plan.ToString(),
                license.Status.ToString(),
                license.IncludedHostUnits,
                license.CurrentHostUnits,
                license.OverageHostUnits,
                license.ValidFrom,
                license.ValidUntil,
                license.GetCapabilities()));
        }
    }
}
