using System.Linq;
using NSubstitute;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetPlatformAdoptionReport;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime;

/// <summary>
/// Testes unitários para Wave AC.3 — GetPlatformAdoptionReport.
/// Cobre AdoptionScore, AdoptionTier, CapabilityAdoptionRate, GrowthOpportunities e Validator.
/// </summary>
public sealed class WaveAcPlatformAdoptionReportTests
{
    private const string TenantId = "tenant-ac3";

    private static GetPlatformAdoptionReport.Handler CreateHandler(IPlatformAdoptionReader reader)
        => new(reader);

    private static IPlatformAdoptionReader EmptyReader()
    {
        var reader = Substitute.For<IPlatformAdoptionReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TeamCapabilityAdoptionEntry>>([]));
        return reader;
    }

    private static IPlatformAdoptionReader ReaderWith(params TeamCapabilityAdoptionEntry[] entries)
    {
        var reader = Substitute.For<IPlatformAdoptionReader>();
        reader.ListByTenantAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TeamCapabilityAdoptionEntry>>(entries));
        return reader;
    }

    private static TeamCapabilityAdoptionEntry AllCapabilities(string team) =>
        new(team, true, true, true, true, true, true, true);

    private static TeamCapabilityAdoptionEntry NoCapabilities(string team) =>
        new(team, false, false, false, false, false, false, false);

    // ── 1. Tenant sem equipas devolve relatório vazio ─────────────────────

    [Fact]
    public async Task Handler_ReturnsEmptyReport_ForEmptyTenant()
    {
        var result = await CreateHandler(EmptyReader())
            .Handle(new GetPlatformAdoptionReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Teams.Should().BeEmpty();
        result.Value.TotalTeams.Should().Be(0);
        result.Value.GlobalAdoptionScore.Should().Be(0.0);
    }

    // ── 2. Equipa com todas as 7 capacidades → score 100 ─────────────────

    [Fact]
    public async Task Handler_Score100_WhenAll7CapabilitiesUsed()
    {
        var result = await CreateHandler(ReaderWith(AllCapabilities("team-a")))
            .Handle(new GetPlatformAdoptionReport.Query(TenantId), CancellationToken.None);

        result.Value.Teams[0].AdoptionScore.Should().Be(100.0);
        result.Value.Teams[0].CapabilitiesUsed.Should().Be(7);
    }

    // ── 3. Equipa com 0 capacidades → score 0 ────────────────────────────

    [Fact]
    public async Task Handler_Score0_WhenNoCapabilitiesUsed()
    {
        var result = await CreateHandler(ReaderWith(NoCapabilities("team-a")))
            .Handle(new GetPlatformAdoptionReport.Query(TenantId), CancellationToken.None);

        result.Value.Teams[0].AdoptionScore.Should().Be(0.0);
        result.Value.Teams[0].CapabilitiesUsed.Should().Be(0);
    }

    // ── 4. Tier Pioneer quando score ≥ 80 ────────────────────────────────

    [Fact]
    public async Task Handler_TierPioneer_WhenScoreAtLeast80()
    {
        var result = await CreateHandler(ReaderWith(AllCapabilities("team-a")))
            .Handle(new GetPlatformAdoptionReport.Query(TenantId), CancellationToken.None);

        result.Value.Teams[0].Tier.Should().Be(GetPlatformAdoptionReport.AdoptionTier.Pioneer);
    }

    // ── 5. Tier Adopter quando score ≥ 60 e < 80 ─────────────────────────

    [Fact]
    public async Task Handler_TierAdopter_WhenScoreBetween60And79()
    {
        // 5/7 capacidades = ~71.4%
        var entry = new TeamCapabilityAdoptionEntry("team-a", true, true, true, true, true, false, false);

        var result = await CreateHandler(ReaderWith(entry))
            .Handle(new GetPlatformAdoptionReport.Query(TenantId), CancellationToken.None);

        result.Value.Teams[0].AdoptionScore.Should().BeApproximately(71.4, 0.1);
        result.Value.Teams[0].Tier.Should().Be(GetPlatformAdoptionReport.AdoptionTier.Adopter);
    }

    // ── 6. Tier Explorer quando score ≥ 40 e < 60 ────────────────────────

    [Fact]
    public async Task Handler_TierExplorer_WhenScoreBetween40And59()
    {
        // 3/7 capacidades = ~42.9%
        var entry = new TeamCapabilityAdoptionEntry("team-a", true, true, true, false, false, false, false);

        var result = await CreateHandler(ReaderWith(entry))
            .Handle(new GetPlatformAdoptionReport.Query(TenantId), CancellationToken.None);

        result.Value.Teams[0].AdoptionScore.Should().BeApproximately(42.9, 0.1);
        result.Value.Teams[0].Tier.Should().Be(GetPlatformAdoptionReport.AdoptionTier.Explorer);
    }

    // ── 7. Tier Laggard quando score < 40 ────────────────────────────────

    [Fact]
    public async Task Handler_TierLaggard_WhenScoreBelow40()
    {
        // 2/7 capacidades = ~28.6%
        var entry = new TeamCapabilityAdoptionEntry("team-a", true, true, false, false, false, false, false);

        var result = await CreateHandler(ReaderWith(entry))
            .Handle(new GetPlatformAdoptionReport.Query(TenantId), CancellationToken.None);

        result.Value.Teams[0].Tier.Should().Be(GetPlatformAdoptionReport.AdoptionTier.Laggard);
    }

    // ── 8. CapabilityAdoptionRate calculada para cada capacidade ─────────

    [Fact]
    public async Task Handler_CapabilityAdoptionRate_ComputedForEachCapability()
    {
        var entries = new[]
        {
            new TeamCapabilityAdoptionEntry("team-a", true, false, false, false, false, false, false),
            new TeamCapabilityAdoptionEntry("team-b", true, true, false, false, false, false, false),
        };

        var result = await CreateHandler(ReaderWith(entries))
            .Handle(new GetPlatformAdoptionReport.Query(TenantId), CancellationToken.None);

        var sloRate = result.Value.CapabilityRates.Single(r => r.CapabilityName == "SloTracking");
        sloRate.TeamsUsing.Should().Be(2);
        sloRate.AdoptionRate.Should().Be(100.0);

        var chaosRate = result.Value.CapabilityRates.Single(r => r.CapabilityName == "ChaosEngineering");
        chaosRate.TeamsUsing.Should().Be(1);
        chaosRate.AdoptionRate.Should().Be(50.0);
    }

    // ── 9. GrowthOpportunities contém apenas capacidades com < 30% adoção ─

    [Fact]
    public async Task Handler_GrowthOpportunities_ContainsCapabilitiesBelow30Percent()
    {
        // 1 de 4 equipas usa ChaosEngineering = 25% < 30%
        var entries = new[]
        {
            new TeamCapabilityAdoptionEntry("t1", true, true, false, false, false, false, false),
            new TeamCapabilityAdoptionEntry("t2", true, false, false, false, false, false, false),
            new TeamCapabilityAdoptionEntry("t3", true, false, false, false, false, false, false),
            new TeamCapabilityAdoptionEntry("t4", true, false, false, false, false, false, false),
        };

        var result = await CreateHandler(ReaderWith(entries))
            .Handle(new GetPlatformAdoptionReport.Query(TenantId), CancellationToken.None);

        result.Value.GrowthOpportunities.Should().Contain("ChaosEngineering");
        result.Value.GrowthOpportunities.Should().NotContain("SloTracking");
    }

    // ── 10. GrowthOpportunities vazio quando todas > 30% ─────────────────

    [Fact]
    public async Task Handler_GrowthOpportunities_EmptyWhenAllAbove30Percent()
    {
        var entries = new[]
        {
            AllCapabilities("t1"),
            AllCapabilities("t2"),
            AllCapabilities("t3"),
        };

        var result = await CreateHandler(ReaderWith(entries))
            .Handle(new GetPlatformAdoptionReport.Query(TenantId), CancellationToken.None);

        result.Value.GrowthOpportunities.Should().BeEmpty();
    }

    // ── 11. GlobalAdoptionScore é a média dos scores de todas as equipas ──

    [Fact]
    public async Task Handler_GlobalAdoptionScore_IsMeanOfAllTeamScores()
    {
        var entries = new[]
        {
            AllCapabilities("t1"),   // 100%
            NoCapabilities("t2"),    // 0%
        };

        var result = await CreateHandler(ReaderWith(entries))
            .Handle(new GetPlatformAdoptionReport.Query(TenantId), CancellationToken.None);

        result.Value.GlobalAdoptionScore.Should().Be(50.0);
    }

    // ── 12. Contagens de tiers são corretas ───────────────────────────────

    [Fact]
    public async Task Handler_TierCounts_AreCorrect()
    {
        var entries = new[]
        {
            AllCapabilities("t1"),                                                                              // Pioneer (100%)
            new TeamCapabilityAdoptionEntry("t2", true, true, true, true, true, false, false),                 // Adopter (~71%)
            new TeamCapabilityAdoptionEntry("t3", true, true, true, false, false, false, false),               // Explorer (~43%)
            NoCapabilities("t4"),                                                                               // Laggard (0%)
        };

        var result = await CreateHandler(ReaderWith(entries))
            .Handle(new GetPlatformAdoptionReport.Query(TenantId), CancellationToken.None);

        result.Value.PioneerCount.Should().Be(1);
        result.Value.AdopterCount.Should().Be(1);
        result.Value.ExplorerCount.Should().Be(1);
        result.Value.LaggardCount.Should().Be(1);
    }

    // ── 13. TotalTeams é correto ──────────────────────────────────────────

    [Fact]
    public async Task Handler_TotalTeams_IsCorrect()
    {
        var entries = new[] { AllCapabilities("t1"), AllCapabilities("t2"), NoCapabilities("t3") };

        var result = await CreateHandler(ReaderWith(entries))
            .Handle(new GetPlatformAdoptionReport.Query(TenantId), CancellationToken.None);

        result.Value.TotalTeams.Should().Be(3);
    }

    // ── 14. Validator rejeita SloLookbackDays < 7 ────────────────────────

    [Fact]
    public void Validator_Rejects_SloLookbackDaysLessThan7()
    {
        var validator = new GetPlatformAdoptionReport.Validator();
        var result = validator.Validate(new GetPlatformAdoptionReport.Query(TenantId, SloLookbackDays: 6));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetPlatformAdoptionReport.Query.SloLookbackDays));
    }
}
