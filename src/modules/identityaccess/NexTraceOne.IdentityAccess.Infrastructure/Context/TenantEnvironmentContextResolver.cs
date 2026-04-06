using Microsoft.Extensions.Logging;

using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Infrastructure.Context;

/// <summary>
/// Implementação de ITenantEnvironmentContextResolver usando os repositórios de Identity.
/// Resolve o contexto operacional completo validando que o ambiente pertence ao tenant.
/// </summary>
internal sealed class TenantEnvironmentContextResolver(
    IEnvironmentRepository environmentRepository,
    ILogger<TenantEnvironmentContextResolver> logger) : ITenantEnvironmentContextResolver
{
    /// <inheritdoc />
    public async Task<TenantEnvironmentContext?> ResolveAsync(
        TenantId tenantId,
        EnvironmentId environmentId,
        CancellationToken cancellationToken = default)
    {
        var environment = await environmentRepository.GetByIdAsync(environmentId, cancellationToken);

        if (environment is null)
        {
            logger.LogWarning(
                "Environment context resolution failed: environment {EnvironmentId} not found for tenant {TenantId}",
                environmentId, tenantId);
            return null;
        }

        // Garantia de isolamento: o ambiente deve pertencer ao tenant ativo.
        if (environment.TenantId != tenantId)
        {
            logger.LogWarning(
                "Environment context resolution failed: environment {EnvironmentId} belongs to tenant {OwnerTenantId}, not {RequestedTenantId} — potential tenant isolation violation",
                environmentId, environment.TenantId, tenantId);
            return null;
        }

        // Ambientes inativos não devem gerar contexto operacional válido.
        if (!environment.IsActive)
        {
            logger.LogWarning(
                "Environment context resolution failed: environment {EnvironmentId} is inactive for tenant {TenantId}",
                environmentId, tenantId);
            return null;
        }

        return TenantEnvironmentContext.From(environment);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantEnvironmentContext>> ListActiveContextsForTenantAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        var environments = await environmentRepository.ListByTenantAsync(tenantId, cancellationToken);

        return environments
            .Where(e => e.IsActive)
            .Select(TenantEnvironmentContext.From)
            .ToList()
            .AsReadOnly();
    }
}
