using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Abstração que fornece estado de saúde real dos subsistemas da plataforma.
/// Implementada na camada de infraestrutura/host usando os health checks reais do ASP.NET Core.
/// </summary>
public interface IPlatformHealthProvider
{
    /// <summary>
    /// Obtém o estado de saúde de todos os subsistemas da plataforma
    /// a partir dos health checks reais registados.
    /// </summary>
    Task<IReadOnlyList<SubsystemHealthInfo>> GetSubsystemHealthAsync(CancellationToken cancellationToken);
}

/// <summary>Estado de saúde de um subsistema, obtido de health checks reais.</summary>
public sealed record SubsystemHealthInfo(
    string Name,
    PlatformSubsystemStatus Status,
    string Description);
