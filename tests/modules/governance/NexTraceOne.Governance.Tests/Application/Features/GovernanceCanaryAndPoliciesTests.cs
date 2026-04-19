using System.Linq;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Governance.Application.Features.GetCanaryRollouts;
using NexTraceOne.Governance.Application.Features.GetCompliancePacks;
using NexTraceOne.Governance.Application.Features.GetEnvironmentPolicies;
using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes unitários para GetCanaryRollouts, GetCompliancePacks e GetEnvironmentPolicies.
/// Cobre integração canary (configurado/não configurado), pacotes de conformidade
/// e políticas de ambiente.
/// </summary>
public sealed class GovernanceCanaryAndPoliciesTests
{
    // ── GetCanaryRollouts ──────────────────────────────────────────────────

    [Fact]
    public async Task GetCanaryRollouts_ProviderNotConfigured_ReturnsEmptyWithSimulatedNote()
    {
        var provider = Substitute.For<ICanaryProvider>();
        provider.IsConfigured.Returns(false);
        provider.GetActiveRolloutsAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<CanaryRolloutInfo>().AsReadOnly());

        var handler = new GetCanaryRollouts.Handler(provider);
        var result = await handler.Handle(new GetCanaryRollouts.Query(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rollouts.Should().BeEmpty();
        result.Value.SimulatedNote.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetCanaryRollouts_ProviderConfigured_ReturnsRollouts()
    {
        var now = DateTimeOffset.UtcNow;
        var rollouts = new List<CanaryRolloutInfo>
        {
            new("rollout-1", "checkout-service", "production",
                "v2.1.0", "v2.0.5", 25, "Running", now.AddHours(-2), null),
            new("rollout-2", "payment-service", "production",
                "v3.0.0", "v2.9.0", 10, "Running", now.AddHours(-1), null),
        };

        var provider = Substitute.For<ICanaryProvider>();
        provider.IsConfigured.Returns(true);
        provider.GetActiveRolloutsAsync(null, Arg.Any<CancellationToken>())
            .Returns(rollouts.AsReadOnly());

        var handler = new GetCanaryRollouts.Handler(provider);
        var result = await handler.Handle(new GetCanaryRollouts.Query(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rollouts.Should().HaveCount(2);
        result.Value.SimulatedNote.Should().BeNull();
    }

    [Fact]
    public async Task GetCanaryRollouts_FilterByEnvironment_PassesEnvironmentToProvider()
    {
        var provider = Substitute.For<ICanaryProvider>();
        provider.IsConfigured.Returns(true);
        provider.GetActiveRolloutsAsync("staging", Arg.Any<CancellationToken>())
            .Returns(new List<CanaryRolloutInfo>().AsReadOnly());

        var handler = new GetCanaryRollouts.Handler(provider);
        var result = await handler.Handle(
            new GetCanaryRollouts.Query("staging", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await provider.Received(1).GetActiveRolloutsAsync("staging", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCanaryRollouts_FilterByStatus_FiltersClientSide()
    {
        var now = DateTimeOffset.UtcNow;
        var rollouts = new List<CanaryRolloutInfo>
        {
            new("r1", "svc-1", "production", "v2", "v1", 20, "Running", now, null),
            new("r2", "svc-2", "production", "v3", "v2", 50, "Completed", now.AddHours(-3), now),
        };

        var provider = Substitute.For<ICanaryProvider>();
        provider.IsConfigured.Returns(true);
        provider.GetActiveRolloutsAsync(null, Arg.Any<CancellationToken>())
            .Returns(rollouts.AsReadOnly());

        var handler = new GetCanaryRollouts.Handler(provider);
        var result = await handler.Handle(
            new GetCanaryRollouts.Query(null, "Running"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rollouts.Should().ContainSingle(r => r.Status == "Running");
    }

    [Fact]
    public async Task GetCanaryRollouts_RolloutsMappedCorrectly()
    {
        var startedAt = DateTimeOffset.UtcNow.AddHours(-1);
        var rollouts = new List<CanaryRolloutInfo>
        {
            new("rollout-1", "catalog-service", "production",
                "v1.5.0", "v1.4.2", 30, "Running", startedAt, null),
        };

        var provider = Substitute.For<ICanaryProvider>();
        provider.IsConfigured.Returns(true);
        provider.GetActiveRolloutsAsync(null, Arg.Any<CancellationToken>())
            .Returns(rollouts.AsReadOnly());

        var handler = new GetCanaryRollouts.Handler(provider);
        var result = await handler.Handle(new GetCanaryRollouts.Query(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Rollouts[0];
        item.Id.Should().Be("rollout-1");
        item.ServiceName.Should().Be("catalog-service");
        item.CanaryVersion.Should().Be("v1.5.0");
        item.StableVersion.Should().Be("v1.4.2");
        item.CanaryTrafficPct.Should().Be(30);
    }

    // ── GetCompliancePacks ─────────────────────────────────────────────────

    [Fact]
    public async Task GetCompliancePacks_NoFrameworkConfig_ReturnsBothPacks()
    {
        var configService = Substitute.For<IConfigurationResolutionService>();
        configService.ResolveEffectiveValueAsync(
                Arg.Any<string>(), Arg.Any<ConfigurationScope>(),
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var handler = new GetCompliancePacks.Handler(configService);
        var result = await handler.Handle(new GetCompliancePacks.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Packs.Should().HaveCount(2, "SOC2TypeII and GDPR are always returned");
        result.Value.Packs.Should().Contain(p => p.PackId == "SOC2TypeII");
        result.Value.Packs.Should().Contain(p => p.PackId == "GDPR");
    }

    [Fact]
    public async Task GetCompliancePacks_SOC2Framework_SOC2PackIsActive()
    {
        var configService = Substitute.For<IConfigurationResolutionService>();
        configService.ResolveEffectiveValueAsync(
                "governance.compliance.framework",
                Arg.Any<ConfigurationScope>(),
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new EffectiveConfigurationDto(
                "governance.compliance.framework", "SOC2TypeII", "system", null, false, false,
                "governance.compliance.framework", "string", false, 1));

        var handler = new GetCompliancePacks.Handler(configService);
        var result = await handler.Handle(new GetCompliancePacks.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var soc2Pack = result.Value.Packs.First(p => p.PackId == "SOC2TypeII");
        soc2Pack.Active.Should().BeTrue();
        var gdprPack = result.Value.Packs.First(p => p.PackId == "GDPR");
        gdprPack.Active.Should().BeFalse();
    }

    [Fact]
    public async Task GetCompliancePacks_PacksHaveControls()
    {
        var configService = Substitute.For<IConfigurationResolutionService>();
        configService.ResolveEffectiveValueAsync(
                Arg.Any<string>(), Arg.Any<ConfigurationScope>(),
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var handler = new GetCompliancePacks.Handler(configService);
        var result = await handler.Handle(new GetCompliancePacks.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Packs.Should().AllSatisfy(p => p.Controls.Should().NotBeEmpty());
    }

    // ── GetEnvironmentPolicies ─────────────────────────────────────────────

    [Fact]
    public async Task GetEnvironmentPolicies_ReturnsThreeDefaultPolicies()
    {
        var handler = new GetEnvironmentPolicies.Handler();
        var result = await handler.Handle(new GetEnvironmentPolicies.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Policies.Should().HaveCount(3);
        result.Value.AvailableEnvironments.Should().Contain("Production");
        result.Value.AvailableEnvironments.Should().Contain("Staging");
        result.Value.AvailableEnvironments.Should().Contain("Development");
    }

    [Fact]
    public async Task GetEnvironmentPolicies_ProductionPolicyRequiresJitForDeploy()
    {
        var handler = new GetEnvironmentPolicies.Handler();
        var result = await handler.Handle(new GetEnvironmentPolicies.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var prodPolicy = result.Value.Policies.First(p => p.Environment == "Production");
        prodPolicy.RequireJitFor.Should().Contain("deploy");
        prodPolicy.JitApprovalRequiredFrom.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetEnvironmentPolicies_DevelopmentPolicyNoJitRequired()
    {
        var handler = new GetEnvironmentPolicies.Handler();
        var result = await handler.Handle(new GetEnvironmentPolicies.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var devPolicy = result.Value.Policies.First(p => p.Environment == "Development");
        devPolicy.RequireJitFor.Should().BeEmpty();
        devPolicy.JitApprovalRequiredFrom.Should().BeNull();
    }

    [Fact]
    public async Task GetEnvironmentPolicies_AllPoliciesHaveAllowedRoles()
    {
        var handler = new GetEnvironmentPolicies.Handler();
        var result = await handler.Handle(new GetEnvironmentPolicies.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Policies.Should().AllSatisfy(p => p.AllowedRoles.Should().NotBeEmpty());
    }
}
