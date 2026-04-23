namespace NexTraceOne.ProductAnalytics.Application.Abstractions;

/// <summary>
/// Leitor de dados de adopção do portal NexTraceOne.
/// Agrega eventos de sessão e feature usage por persona, equipa e módulo para calcular o funil de adopção.
/// </summary>
public interface IPortalAdoptionReader
{
    /// <summary>Lista entradas de adopção por equipa para o período especificado.</summary>
    Task<IReadOnlyList<TeamAdoptionEntry>> ListTeamAdoptionAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    /// <summary>Lista utilizadores inativos (sem login no período).</summary>
    Task<IReadOnlyList<InactiveUserEntry>> ListInactiveUsersAsync(
        string tenantId,
        DateTimeOffset since,
        CancellationToken cancellationToken);

    /// <summary>Retorna tendência de adopção nos últimos 90 dias.</summary>
    Task<IReadOnlyList<DailyAdoptionSnapshot>> GetAdoptionTrendAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    /// <summary>Entrada de adopção por equipa.</summary>
    public sealed record TeamAdoptionEntry(
        string TeamId,
        string TeamName,
        int TotalMembers,
        IReadOnlyList<FeatureAdoptionStat> FeatureStats,
        DateTimeOffset? LastActiveAt);

    /// <summary>Estatísticas de adopção por feature para uma equipa.</summary>
    public sealed record FeatureAdoptionStat(
        string FeatureName,
        int AwareUsers,
        int ActiveUsers,
        int PowerUsers);

    /// <summary>Utilizador inativo com detalhes de licença.</summary>
    public sealed record InactiveUserEntry(
        string UserId,
        string UserName,
        string TeamName,
        DateTimeOffset? LastLoginAt);

    /// <summary>Snapshot diário de adopção.</summary>
    public sealed record DailyAdoptionSnapshot(
        int DaysAgo,
        int ActiveUsers,
        int TotalLicensedUsers);
}
