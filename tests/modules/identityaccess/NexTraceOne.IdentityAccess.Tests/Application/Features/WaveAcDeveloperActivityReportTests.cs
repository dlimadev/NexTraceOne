using NSubstitute;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Features.GetDeveloperActivityReport;

namespace NexTraceOne.IdentityAccess.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave AC.2 — GetDeveloperActivityReport.
/// Cobre ponderação de TotalActions, ActivityTier por percentil, TeamSummary e Validator.
/// </summary>
public sealed class WaveAcDeveloperActivityReportTests
{
    private const string TenantId = "tenant-ac2";

    private static GetDeveloperActivityReport.Handler CreateHandler(IDeveloperActivityReader reader)
        => new(reader);

    private static IDeveloperActivityReader EmptyReader()
    {
        var reader = Substitute.For<IDeveloperActivityReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<DeveloperActivityEntry>>([]));
        return reader;
    }

    private static IDeveloperActivityReader ReaderWith(params DeveloperActivityEntry[] entries)
    {
        var reader = Substitute.For<IDeveloperActivityReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<DeveloperActivityEntry>>(entries));
        return reader;
    }

    private static DeveloperActivityEntry MakeEntry(
        string userId,
        string? team = "team-a",
        int contractsCreated = 0,
        int contractsUpdated = 0,
        int runbooksCreated = 0,
        int runbooksUpdated = 0,
        int releasesRegistered = 0,
        int operationalNotes = 0)
        => new(userId, userId, team, contractsCreated, contractsUpdated,
            runbooksCreated, runbooksUpdated, releasesRegistered, operationalNotes);

    // ── 1. Tenant sem utilizadores devolve relatório vazio ────────────────

    [Fact]
    public async Task Handler_ReturnsEmptyReport_ForEmptyTenant()
    {
        var result = await CreateHandler(EmptyReader())
            .Handle(new GetDeveloperActivityReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Developers.Should().BeEmpty();
        result.Value.TotalDevelopers.Should().Be(0);
    }

    // ── 2. TotalActions ponderado corretamente (contratos=3, runbooks=2, rest=1) ──

    [Fact]
    public async Task Handler_TotalActions_WeightedCorrectly()
    {
        // contratos: (2+3)*3=15, runbooks: (1+1)*2=4, releases=2, notes=1 → total=22
        var entry = MakeEntry("u1", contractsCreated: 2, contractsUpdated: 3,
            runbooksCreated: 1, runbooksUpdated: 1, releasesRegistered: 2, operationalNotes: 1);

        var result = await CreateHandler(ReaderWith(entry))
            .Handle(new GetDeveloperActivityReport.Query(TenantId), CancellationToken.None);

        result.Value.Developers[0].TotalActions.Should().Be(22);
    }

    // ── 3. Utilizador inativo tem TotalActions=0 e tier Inactive ─────────

    [Fact]
    public async Task Handler_InactiveUser_TotalActionsZeroAndInactiveTier()
    {
        var entry = MakeEntry("u1");

        var result = await CreateHandler(ReaderWith(entry))
            .Handle(new GetDeveloperActivityReport.Query(TenantId), CancellationToken.None);

        result.Value.Developers[0].TotalActions.Should().Be(0);
        result.Value.Developers[0].Tier.Should().Be(GetDeveloperActivityReport.ActivityTier.Inactive);
    }

    // ── 4. Tier HighlyActive para utilizadores acima de P75 ──────────────

    [Fact]
    public async Task Handler_HighlyActive_ForUsersAboveP75()
    {
        // 4 utilizadores: actions 1, 2, 3, 10
        // P75 (percentil 75 de 4 = posição 3 = valor 3) → usuário com 10 ≥ 3 → HighlyActive
        var entries = new[]
        {
            MakeEntry("u1", releasesRegistered: 1),
            MakeEntry("u2", releasesRegistered: 2),
            MakeEntry("u3", releasesRegistered: 3),
            MakeEntry("u4", releasesRegistered: 10),
        };

        var result = await CreateHandler(ReaderWith(entries))
            .Handle(new GetDeveloperActivityReport.Query(TenantId), CancellationToken.None);

        var u4 = result.Value.Developers.Single(d => d.UserId == "u4");
        u4.Tier.Should().Be(GetDeveloperActivityReport.ActivityTier.HighlyActive);
    }

    // ── 5. Tier Active para utilizadores acima de P50 ────────────────────

    [Fact]
    public async Task Handler_Active_ForUsersAboveP50()
    {
        // 4 utilizadores: actions 1, 2, 5, 10
        // P50 = posição 2 = valor 2, P75 = posição 3 = valor 5
        // u3 com 5 ≥ P75(5) → HighlyActive; u2 com 2 = P50 → Active
        var entries = new[]
        {
            MakeEntry("u1", releasesRegistered: 1),
            MakeEntry("u2", releasesRegistered: 2),
            MakeEntry("u3", releasesRegistered: 5),
            MakeEntry("u4", releasesRegistered: 10),
        };

        var result = await CreateHandler(ReaderWith(entries))
            .Handle(new GetDeveloperActivityReport.Query(TenantId), CancellationToken.None);

        var u2 = result.Value.Developers.Single(d => d.UserId == "u2");
        u2.Tier.Should().Be(GetDeveloperActivityReport.ActivityTier.Active);
    }

    // ── 6. Tier Occasional para utilizadores com 1+ ação abaixo de P50 ───

    [Fact]
    public async Task Handler_Occasional_ForUsersWithActionsBelow_P50()
    {
        // 4 utilizadores: actions 1, 5, 8, 10 → P50=posição 2=5
        // u1 com 1 < P50(5) e TotalActions > 0 → Occasional
        var entries = new[]
        {
            MakeEntry("u1", releasesRegistered: 1),
            MakeEntry("u2", releasesRegistered: 5),
            MakeEntry("u3", releasesRegistered: 8),
            MakeEntry("u4", releasesRegistered: 10),
        };

        var result = await CreateHandler(ReaderWith(entries))
            .Handle(new GetDeveloperActivityReport.Query(TenantId), CancellationToken.None);

        var u1 = result.Value.Developers.Single(d => d.UserId == "u1");
        u1.Tier.Should().Be(GetDeveloperActivityReport.ActivityTier.Occasional);
    }

    // ── 7. InactiveTeamNames inclui equipas onde todos os membros são Inactive ──

    [Fact]
    public async Task Handler_InactiveTeamNames_IncludesTeamsWhereAllMembersInactive()
    {
        var entries = new[]
        {
            MakeEntry("u1", team: "team-dead"),
            MakeEntry("u2", team: "team-dead"),
            MakeEntry("u3", team: "team-alive", releasesRegistered: 5),
        };

        var result = await CreateHandler(ReaderWith(entries))
            .Handle(new GetDeveloperActivityReport.Query(TenantId), CancellationToken.None);

        result.Value.InactiveTeamNames.Should().Contain("team-dead");
        result.Value.InactiveTeamNames.Should().NotContain("team-alive");
    }

    // ── 8. InactiveTeamNames exclui equipas com pelo menos um membro ativo ─

    [Fact]
    public async Task Handler_InactiveTeamNames_ExcludesTeamsWithActiveMembers()
    {
        var entries = new[]
        {
            MakeEntry("u1", team: "team-mixed"),
            MakeEntry("u2", team: "team-mixed", releasesRegistered: 3),
        };

        var result = await CreateHandler(ReaderWith(entries))
            .Handle(new GetDeveloperActivityReport.Query(TenantId), CancellationToken.None);

        result.Value.InactiveTeamNames.Should().NotContain("team-mixed");
    }

    // ── 9. TopActiveDevelopers limitado a 10 ────────────────────────────

    [Fact]
    public async Task Handler_TopActiveDevelopers_LimitedTo10()
    {
        var entries = Enumerable.Range(1, 15)
            .Select(i => MakeEntry($"u{i}", releasesRegistered: i))
            .ToArray();

        var result = await CreateHandler(ReaderWith(entries))
            .Handle(new GetDeveloperActivityReport.Query(TenantId), CancellationToken.None);

        result.Value.TopActiveDevelopers.Count.Should().Be(10);
    }

    // ── 10. TopActiveTeams com agrupamento e soma corretos ───────────────

    [Fact]
    public async Task Handler_TopActiveTeams_CorrectGroupingAndSum()
    {
        var entries = new[]
        {
            MakeEntry("u1", team: "team-a", releasesRegistered: 3),
            MakeEntry("u2", team: "team-a", releasesRegistered: 2),
            MakeEntry("u3", team: "team-b", releasesRegistered: 10),
        };

        var result = await CreateHandler(ReaderWith(entries))
            .Handle(new GetDeveloperActivityReport.Query(TenantId), CancellationToken.None);

        var teamA = result.Value.TopActiveTeams.Single(t => t.TeamName == "team-a");
        teamA.TotalActions.Should().Be(5);
        teamA.MemberCount.Should().Be(2);

        var teamB = result.Value.TopActiveTeams.Single(t => t.TeamName == "team-b");
        teamB.TotalActions.Should().Be(10);
    }

    // ── 11. LookbackDays é propagado corretamente para o relatório ────────

    [Fact]
    public async Task Handler_LookbackDays_PassedThroughToReport()
    {
        var result = await CreateHandler(EmptyReader())
            .Handle(new GetDeveloperActivityReport.Query(TenantId, LookbackDays: 14), CancellationToken.None);

        result.Value.LookbackDays.Should().Be(14);
    }

    // ── 12. Validator rejeita LookbackDays < 7 ───────────────────────────

    [Fact]
    public void Validator_Rejects_LookbackDaysLessThan7()
    {
        var validator = new GetDeveloperActivityReport.Validator();
        var result = validator.Validate(new GetDeveloperActivityReport.Query(TenantId, LookbackDays: 6));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetDeveloperActivityReport.Query.LookbackDays));
    }
}
