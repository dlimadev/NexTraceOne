using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de sessões de onboarding assistido por IA.
/// Suporta consulta por utilizador, equipa e estado para acompanhamento do progresso.
/// </summary>
public interface IOnboardingSessionRepository
{
    /// <summary>Adiciona uma nova sessão de onboarding para persistência.</summary>
    Task AddAsync(OnboardingSession session, CancellationToken ct = default);

    /// <summary>Obtém uma sessão pelo identificador.</summary>
    Task<OnboardingSession?> GetByIdAsync(OnboardingSessionId id, CancellationToken ct = default);

    /// <summary>Lista sessões de onboarding com filtros opcionais por equipa e estado.</summary>
    Task<IReadOnlyList<OnboardingSession>> ListAsync(Guid? teamId, OnboardingSessionStatus? status, CancellationToken ct = default);

    /// <summary>Atualiza uma sessão de onboarding existente.</summary>
    Task UpdateAsync(OnboardingSession session, CancellationToken ct = default);
}
