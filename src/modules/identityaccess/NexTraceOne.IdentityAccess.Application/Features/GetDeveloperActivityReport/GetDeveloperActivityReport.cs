using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Application.Features.GetDeveloperActivityReport;

/// <summary>
/// Feature: GetDeveloperActivityReport — atividade de developers na plataforma NexTraceOne.
///
/// Calcula TotalActions ponderado por tipo de ação:
/// - Contratos criados/atualizados: peso 3
/// - Runbooks criados/atualizados: peso 2
/// - Releases registados e notas operacionais criadas: peso 1
///
/// Atribui <see cref="ActivityTier"/> por percentil:
/// - HighlyActive: ≥ P75 e TotalActions &gt; 0
/// - Active: ≥ P50 e &lt; P75
/// - Occasional: TotalActions ≥ 1 e &lt; P50
/// - Inactive: TotalActions = 0
///
/// Produz sumários de equipa e identifica equipas inteiramente inativas.
///
/// Wave AC.2 — GetDeveloperActivityReport (IdentityAccess).
/// </summary>
public static class GetDeveloperActivityReport
{
    // ── Tier de atividade ─────────────────────────────────────────────────

    /// <summary>Nível de atividade de um developer na plataforma.</summary>
    public enum ActivityTier
    {
        /// <summary>Ações ≥ P75 e TotalActions &gt; 0.</summary>
        HighlyActive,
        /// <summary>Ações ≥ P50 e &lt; P75.</summary>
        Active,
        /// <summary>TotalActions ≥ 1 e &lt; P50.</summary>
        Occasional,
        /// <summary>Sem ações no período.</summary>
        Inactive
    }

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Sumário de atividade de um developer.</summary>
    public sealed record DeveloperActivitySummary(
        string UserId,
        string UserName,
        string? TeamName,
        int ContractsCreated,
        int ContractsUpdated,
        int RunbooksCreated,
        int RunbooksUpdated,
        int ReleasesRegistered,
        int OperationalNotesCreated,
        int TotalActions,
        ActivityTier Tier);

    /// <summary>Sumário de atividade de uma equipa.</summary>
    public sealed record TeamActivitySummary(
        string TeamName,
        int TotalActions,
        int MemberCount,
        double AvgActionsPerMember);

    /// <summary>Relatório completo de atividade de developers do tenant.</summary>
    public sealed record Report(
        string TenantId,
        IReadOnlyList<DeveloperActivitySummary> Developers,
        IReadOnlyList<DeveloperActivitySummary> TopActiveDevelopers,
        IReadOnlyList<TeamActivitySummary> TopActiveTeams,
        IReadOnlyList<string> InactiveTeamNames,
        int TotalDevelopers,
        int InactiveDevelopersCount,
        int LookbackDays);

    // ── Query ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: período de análise em dias (7–90, padrão 30).</para>
    /// <para><c>HighlyActivePercentile</c>: percentil a partir do qual o tier é HighlyActive (50–95, padrão 75).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        int HighlyActivePercentile = 75) : IQuery<Report>;

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(7, 90);
            RuleFor(q => q.HighlyActivePercentile).InclusiveBetween(50, 95);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IDeveloperActivityReader _reader;

        public Handler(IDeveloperActivityReader reader)
        {
            _reader = Guard.Against.Null(reader);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var entries = await _reader.ListByTenantAsync(query.TenantId, query.LookbackDays, cancellationToken);

            if (entries.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    TenantId: query.TenantId,
                    Developers: [],
                    TopActiveDevelopers: [],
                    TopActiveTeams: [],
                    InactiveTeamNames: [],
                    TotalDevelopers: 0,
                    InactiveDevelopersCount: 0,
                    LookbackDays: query.LookbackDays));
            }

            // Calcula TotalActions ponderado para cada developer
            var withActions = entries
                .Select(e => (Entry: e, TotalActions: ComputeWeightedActions(e)))
                .ToList();

            // Calcula percentis P75 e P50 a partir das ações
            var sortedActions = withActions.Select(x => x.TotalActions).OrderBy(a => a).ToList();
            var p75 = ComputePercentileValue(sortedActions, query.HighlyActivePercentile);
            var p50 = ComputePercentileValue(sortedActions, 50);

            // Atribui tier a cada developer
            var summaries = withActions
                .Select(x => new DeveloperActivitySummary(
                    UserId: x.Entry.UserId,
                    UserName: x.Entry.UserName,
                    TeamName: x.Entry.TeamName,
                    ContractsCreated: x.Entry.ContractsCreated,
                    ContractsUpdated: x.Entry.ContractsUpdated,
                    RunbooksCreated: x.Entry.RunbooksCreated,
                    RunbooksUpdated: x.Entry.RunbooksUpdated,
                    ReleasesRegistered: x.Entry.ReleasesRegistered,
                    OperationalNotesCreated: x.Entry.OperationalNotesCreated,
                    TotalActions: x.TotalActions,
                    Tier: AssignTier(x.TotalActions, p75, p50)))
                .ToList();

            var topDevelopers = summaries
                .OrderByDescending(s => s.TotalActions)
                .Take(10)
                .ToList();

            // Sumários por equipa
            var teamSummaries = summaries
                .Where(s => !string.IsNullOrWhiteSpace(s.TeamName))
                .GroupBy(s => s.TeamName!)
                .Select(g =>
                {
                    var total = g.Sum(s => s.TotalActions);
                    var count = g.Count();
                    return new TeamActivitySummary(
                        TeamName: g.Key,
                        TotalActions: total,
                        MemberCount: count,
                        AvgActionsPerMember: count > 0 ? (double)total / count : 0.0);
                })
                .ToList();

            var topTeams = teamSummaries
                .OrderByDescending(t => t.TotalActions)
                .Take(10)
                .ToList();

            // Equipas inteiramente inativas (todos os membros são Inactive)
            var inactiveTeams = summaries
                .Where(s => !string.IsNullOrWhiteSpace(s.TeamName))
                .GroupBy(s => s.TeamName!)
                .Where(g => g.All(s => s.Tier == ActivityTier.Inactive))
                .Select(g => g.Key)
                .OrderBy(name => name)
                .ToList();

            return Result<Report>.Success(new Report(
                TenantId: query.TenantId,
                Developers: summaries,
                TopActiveDevelopers: topDevelopers,
                TopActiveTeams: topTeams,
                InactiveTeamNames: inactiveTeams,
                TotalDevelopers: summaries.Count,
                InactiveDevelopersCount: summaries.Count(s => s.Tier == ActivityTier.Inactive),
                LookbackDays: query.LookbackDays));
        }

        // ── Cálculo de ações ponderadas ───────────────────────────────────

        private static int ComputeWeightedActions(DeveloperActivityEntry entry) =>
            (entry.ContractsCreated + entry.ContractsUpdated) * 3 +
            (entry.RunbooksCreated + entry.RunbooksUpdated) * 2 +
            entry.ReleasesRegistered +
            entry.OperationalNotesCreated;

        // ── Cálculo de percentil ──────────────────────────────────────────

        private static int ComputePercentileValue(IReadOnlyList<int> sortedValues, int percentile)
        {
            if (sortedValues.Count == 0)
                return 0;

            // Posição do percentil (índice baseado em 0)
            var index = (int)Math.Ceiling(percentile / 100.0 * sortedValues.Count) - 1;
            index = Math.Clamp(index, 0, sortedValues.Count - 1);
            return sortedValues[index];
        }

        // ── Atribuição de tier ────────────────────────────────────────────

        private static ActivityTier AssignTier(int totalActions, int p75, int p50)
        {
            if (totalActions == 0)
                return ActivityTier.Inactive;
            if (totalActions >= p75 && totalActions > 0)
                return ActivityTier.HighlyActive;
            if (totalActions >= p50)
                return ActivityTier.Active;
            return ActivityTier.Occasional;
        }
    }
}
