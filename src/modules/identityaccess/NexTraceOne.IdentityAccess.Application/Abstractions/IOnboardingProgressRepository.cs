using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>Repositório de progresso de onboarding por tenant.</summary>
public interface IOnboardingProgressRepository
{
    /// <summary>Obtém o progresso de onboarding de um tenant pelo seu identificador.</summary>
    Task<OnboardingProgress?> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo registo de progresso.</summary>
    Task AddAsync(OnboardingProgress progress, CancellationToken cancellationToken);

    /// <summary>Actualiza o registo de progresso existente.</summary>
    Task UpdateAsync(OnboardingProgress progress, CancellationToken cancellationToken);
}
