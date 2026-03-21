using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Infrastructure.Context;

/// <summary>
/// Implementação do contexto operacional de execução.
/// Populado pelo EnvironmentResolutionMiddleware após resolução completa de tenant e ambiente.
/// Acessa ICurrentTenant e ICurrentUser que já foram resolvidos anteriormente na pipeline.
/// </summary>
public sealed class OperationalExecutionContext(
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    IEnvironmentContextAccessor environmentContextAccessor) : IOperationalExecutionContext
{
    /// <inheritdoc />
    public string UserId => currentUser.Id;

    /// <inheritdoc />
    public string UserName => currentUser.Name;

    /// <inheritdoc />
    public string UserEmail => currentUser.Email;

    /// <inheritdoc />
    public TenantId TenantId => new(currentTenant.Id);

    /// <inheritdoc />
    public EnvironmentId EnvironmentId => environmentContextAccessor.EnvironmentId;

    /// <inheritdoc />
    public EnvironmentProfile EnvironmentProfile => environmentContextAccessor.Profile;

    /// <inheritdoc />
    public bool IsProductionLikeEnvironment => environmentContextAccessor.IsProductionLike;

    /// <inheritdoc />
    public TenantEnvironmentContext TenantEnvironmentContext
        => TenantEnvironmentContext.Create(
            TenantId,
            EnvironmentId,
            EnvironmentProfile,
            InferCriticality(EnvironmentProfile, IsProductionLikeEnvironment),
            IsProductionLikeEnvironment,
            isActive: true);

    /// <summary>
    /// Infere a criticidade a partir do perfil e do flag IsProductionLike.
    /// Usado quando o TenantEnvironmentContext é construído a partir do contexto de execução,
    /// sem acesso direto à entidade de domínio (que contém a criticidade real).
    /// Em fases futuras, o accessor de ambiente deverá expor a criticidade diretamente.
    /// </summary>
    private static EnvironmentCriticality InferCriticality(EnvironmentProfile profile, bool isProductionLike)
    {
        if (isProductionLike)
            return EnvironmentCriticality.Critical;

        return profile switch
        {
            EnvironmentProfile.Production => EnvironmentCriticality.Critical,
            EnvironmentProfile.DisasterRecovery => EnvironmentCriticality.Critical,
            EnvironmentProfile.Staging => EnvironmentCriticality.High,
            EnvironmentProfile.UserAcceptanceTesting => EnvironmentCriticality.High,
            EnvironmentProfile.Validation => EnvironmentCriticality.Medium,
            EnvironmentProfile.PerformanceTesting => EnvironmentCriticality.Medium,
            _ => EnvironmentCriticality.Low
        };
    }

    /// <inheritdoc />
    public bool IsFullyResolved
        => currentTenant.Id != Guid.Empty
           && currentTenant.IsActive
           && environmentContextAccessor.IsResolved
           && currentUser.IsAuthenticated;

    /// <inheritdoc />
    public bool HasTenantContext
        => currentTenant.Id != Guid.Empty && currentTenant.IsActive;
}
